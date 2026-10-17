import * as FileSystem from 'expo-file-system/legacy';

const ROOT = `${FileSystem.documentDirectory}customer-documents/`;
const INDEX = `${ROOT}index.json`;

export type LocalCustomerDocument = {
  documentId: string;
  fileName: string;
  contentType: string;
  fileSizeBytes: number;
  caseId?: string;
  appointmentId?: string;
  received?: boolean;
};

async function ensureRoot() {
  const info = await FileSystem.getInfoAsync(ROOT);
  if (!info.exists) {
    await FileSystem.makeDirectoryAsync(ROOT, { intermediates: true });
  }
}

export function localDocumentUri(documentId: string) {
  return `${ROOT}${documentId}`;
}

export async function readLocalDocuments(): Promise<LocalCustomerDocument[]> {
  try {
    const raw = await FileSystem.readAsStringAsync(INDEX);
    const parsed = JSON.parse(raw) as LocalCustomerDocument[];
    return Array.isArray(parsed) ? parsed : [];
  } catch {
    return [];
  }
}

async function writeIndex(items: LocalCustomerDocument[]) {
  await ensureRoot();
  await FileSystem.writeAsStringAsync(INDEX, JSON.stringify(items));
}

export async function storeOwnedDocument(input: {
  sourceUri: string;
  documentId: string;
  fileName: string;
  contentType: string;
  fileSizeBytes: number;
  caseId?: string;
  appointmentId?: string;
}) {
  await ensureRoot();
  const dest = localDocumentUri(input.documentId);
  await FileSystem.copyAsync({ from: input.sourceUri, to: dest });
  const items = await readLocalDocuments();
  const next = items.filter((item) => item.documentId !== input.documentId);
  next.push({
    documentId: input.documentId,
    fileName: input.fileName,
    contentType: input.contentType,
    fileSizeBytes: input.fileSizeBytes,
    caseId: input.caseId,
    appointmentId: input.appointmentId,
  });
  await writeIndex(next);
}

export async function storeReceivedDocument(input: {
  documentId: string;
  fileName: string;
  contentType: string;
  fileSizeBytes: number;
  tempUri: string;
}) {
  await ensureRoot();
  const dest = `${ROOT}received-${input.documentId}`;
  await FileSystem.moveAsync({ from: input.tempUri, to: dest }).catch(async () => {
    await FileSystem.copyAsync({ from: input.tempUri, to: dest });
  });
  const items = await readLocalDocuments();
  const next = items.filter((item) => !(item.documentId === input.documentId && item.received));
  next.push({
    documentId: input.documentId,
    fileName: input.fileName,
    contentType: input.contentType,
    fileSizeBytes: input.fileSizeBytes,
    received: true,
  });
  await writeIndex(next);
  return dest;
}
