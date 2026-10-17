import React, { useCallback, useEffect, useMemo, useState } from 'react';
import { View } from 'react-native';
import * as DocumentPicker from 'expo-document-picker';
import * as FileSystem from 'expo-file-system/legacy';
import { useQuery, useQueryClient } from '@tanstack/react-query';

import { Button } from '@/components/ui/Button';
import { Card } from '@/components/ui/Card';
import { Text } from '@/components/ui/Text';
import { getApiBaseUrl } from '@/config/apiBaseUrl';
import { customerDocumentsApi, type CustomerDocumentItem } from '@/services/api/customerDocumentsApi';
import {
  localDocumentUri,
  readLocalDocuments,
  storeOwnedDocument,
  storeReceivedDocument,
} from '@/services/documents/localCustomerDocuments';
import { setDocumentHubHandlers, type DocumentTransferNotice } from '@/services/realtime/documentHub';
import { useAuthStore } from '@/store/authStore';
import { useToastStore } from '@/store/toastStore';
import { useTheme } from '@/theme/ThemeProvider';
import { getErrorMessage } from '@/utils/errors';

const PICKER_TYPES = [
  'application/pdf',
  'application/msword',
  'application/vnd.openxmlformats-officedocument.wordprocessingml.document',
  'image/jpeg',
  'image/png',
];

function normalizeType(contentType: string) {
  const value = contentType.toLowerCase();
  return value === 'image/jpg' ? 'image/jpeg' : value;
}

export function SharedDocumentsPanel({
  caseId,
  appointmentId,
  asLawyer = false,
}: {
  caseId?: string;
  appointmentId?: string;
  asLawyer?: boolean;
}) {
  const theme = useTheme();
  const showToast = useToastStore((s) => s.show);
  const queryClient = useQueryClient();
  const isCustomer = !asLawyer;
  const [busy, setBusy] = useState(false);
  const scope = caseId ? { caseId } : { appointmentId };
  const queryKey = useMemo(
    () => ['customer-documents', caseId ?? null, appointmentId ?? null] as const,
    [caseId, appointmentId],
  );

  const documents = useQuery({
    queryKey,
    queryFn: () => customerDocumentsApi.list(scope),
    enabled: Boolean(caseId || appointmentId),
    refetchInterval: 8000,
  });

  const refresh = useCallback(() => {
    void queryClient.invalidateQueries({ queryKey });
  }, [queryClient, queryKey]);

  const downloadReady = useCallback(
    async (notice: DocumentTransferNotice) => {
      const item = documents.data?.find((doc) => doc.documentId === notice.documentId);
      const access = useAuthStore.getState().session?.accessToken;
      if (!item || !access) return;
      const temp = `${FileSystem.cacheDirectory}incoming-${notice.documentId}`;
      const url = `${getApiBaseUrl()}/api/customer-documents/transfers/${notice.sessionId}/download`;
      const result = await FileSystem.downloadAsync(url, temp, {
        headers: { Authorization: `Bearer ${access}` },
      });
      if (result.status < 200 || result.status >= 300) {
        throw new Error(`Download failed (${result.status})`);
      }
      const saved = await storeReceivedDocument({
        documentId: notice.documentId,
        fileName: item.fileName,
        contentType: item.contentType,
        fileSizeBytes: item.fileSizeBytes,
        tempUri: result.uri,
      });
      showToast({ title: 'Document saved', message: saved, variant: 'success' });
      refresh();
    },
    [documents.data, refresh, showToast],
  );

  useEffect(() => {
    setDocumentHubHandlers({
      onRequested: () => refresh(),
      onReady: (notice) => {
        if (isCustomer) return;
        void downloadReady(notice).catch((error) => {
          showToast({ title: 'Transfer failed', message: getErrorMessage(error), variant: 'error' });
        });
      },
    });
  }, [downloadReady, isCustomer, refresh, showToast]);

  const shareFromPhone = useCallback(async () => {
    const picked = await DocumentPicker.getDocumentAsync({ type: PICKER_TYPES, copyToCacheDirectory: true });
    if (picked.canceled || !picked.assets[0]) return;
    const asset = picked.assets[0];
    const contentType = normalizeType(asset.mimeType ?? 'application/pdf');
    const fileSizeBytes = asset.size ?? 0;
    if (fileSizeBytes <= 0) {
      showToast({ title: 'Empty file', message: 'Choose a non-empty document.', variant: 'error' });
      return;
    }
    setBusy(true);
    try {
      const registered = await customerDocumentsApi.register({
        caseId,
        appointmentId,
        fileName: asset.name,
        contentType,
        fileSizeBytes,
      });
      await storeOwnedDocument({
        sourceUri: asset.uri,
        documentId: registered.documentId,
        fileName: registered.fileName,
        contentType: registered.contentType,
        fileSizeBytes: registered.fileSizeBytes,
        caseId,
        appointmentId,
      });
      showToast({ title: 'Saved on this phone', message: 'A lawyer can request it while you are online.', variant: 'success' });
      refresh();
    } catch (error) {
      showToast({ title: 'Could not save', message: getErrorMessage(error), variant: 'error' });
    } finally {
      setBusy(false);
    }
  }, [appointmentId, caseId, refresh, showToast]);

  const approve = useCallback(
    async (item: CustomerDocumentItem) => {
      if (!item.openTransferId) return;
      const local = (await readLocalDocuments()).find((doc) => doc.documentId === item.documentId && !doc.received);
      if (!local) {
        showToast({ title: 'File missing', message: 'That document is no longer on this phone.', variant: 'error' });
        return;
      }
      setBusy(true);
      try {
        const approved = await customerDocumentsApi.approve(item.openTransferId);
        const access = useAuthStore.getState().session?.accessToken;
        const url = approved.uploadPath.startsWith('http')
          ? approved.uploadPath
          : `${getApiBaseUrl()}${approved.uploadPath}`;
        const upload = await FileSystem.uploadAsync(url, localDocumentUri(item.documentId), {
          httpMethod: 'POST',
          uploadType: FileSystem.FileSystemUploadType.BINARY_CONTENT,
          headers: {
            Authorization: `Bearer ${access ?? ''}`,
            'X-Transfer-Token': approved.uploadToken,
            'Content-Type': item.contentType,
          },
        });
        if (upload.status < 200 || upload.status >= 300) {
          throw new Error(`Transfer failed (${upload.status})`);
        }
        showToast({ title: 'Sent', message: 'The lawyer received the document.', variant: 'success' });
        refresh();
      } catch (error) {
        showToast({ title: 'Transfer failed', message: getErrorMessage(error), variant: 'error' });
      } finally {
        setBusy(false);
      }
    },
    [refresh, showToast],
  );

  return (
    <View style={{ gap: theme.spacing.sm, marginTop: theme.spacing.md }}>
      <Text variant="bodyMedium">On this phone</Text>
      <Text variant="caption" color="secondary">
        The file stays on the customer phone. Keep that app open until the transfer finishes. A lawyer can download it only while the customer is online and approves.
      </Text>
      {isCustomer ? <Button title="Share a document" variant="secondary" loading={busy} onPress={() => void shareFromPhone()} /> : null}
      {(documents.data ?? []).map((item) => (
        <Card key={item.documentId}>
          <Text variant="bodyMedium">{item.fileName}</Text>
          <Text variant="caption" color="secondary">
            {(item.fileSizeBytes / 1024).toFixed(0)} KB
            {item.openTransferStatus ? ` · ${item.openTransferStatus}` : ''}
          </Text>
          {isCustomer && item.openTransferStatus === 'Pending' && item.openTransferId ? (
            <View style={{ flexDirection: 'row', gap: theme.spacing.sm, marginTop: theme.spacing.sm }}>
              <Button title="Approve and send" size="sm" fullWidth={false} loading={busy} onPress={() => void approve(item)} />
              <Button
                title="Reject"
                size="sm"
                fullWidth={false}
                variant="ghost"
                onPress={() =>
                  void customerDocumentsApi.reject(item.openTransferId!).then(refresh).catch((error) => {
                    showToast({ title: 'Could not reject', message: getErrorMessage(error), variant: 'error' });
                  })
                }
              />
            </View>
          ) : null}
          {!isCustomer ? (
            <Button
              title="Request"
              size="sm"
              fullWidth={false}
              variant="secondary"
              loading={busy}
              disabled={item.openTransferStatus === 'Pending' || item.openTransferStatus === 'Approved' || item.openTransferStatus === 'Transferring'}
              onPress={() =>
                void customerDocumentsApi
                  .request(item.documentId)
                  .then(() => {
                    showToast({ title: 'Request sent', message: 'Waiting for the customer to approve.', variant: 'success' });
                    refresh();
                  })
                  .catch((error) => showToast({ title: 'Request failed', message: getErrorMessage(error), variant: 'error' }))
              }
            />
          ) : null}
        </Card>
      ))}
      {!documents.isLoading && (documents.data ?? []).length === 0 ? (
        <Text variant="caption" color="secondary">No phone documents yet.</Text>
      ) : null}
    </View>
  );
}
