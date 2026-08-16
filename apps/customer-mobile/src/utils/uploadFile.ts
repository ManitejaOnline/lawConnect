/**
 * A picked file, as returned by expo-image-picker / expo-document-picker.
 * On web those APIs also return `file` (a real web File) alongside `uri` —
 * `uri` there is a blob/data URL that RN's native `{ uri, name, type }`
 * FormData convention cannot describe to a real browser FormData/XHR.
 */
export type PickedFile = {
  uri: string;
  name: string;
  mimeType: string;
  file?: File;
};

/**
 * Appends a picked file to FormData in whichever shape the current platform's
 * networking layer actually understands: a real File on web, the RN
 * `{ uri, name, type }` object on native.
 */
export function appendPickedFile(form: FormData, field: string, picked: PickedFile): void {
  if (picked.file) {
    form.append(field, picked.file, picked.name);
    return;
  }

  form.append(field, {
    uri: picked.uri,
    name: picked.name,
    type: picked.mimeType,
  } as unknown as Blob);
}
