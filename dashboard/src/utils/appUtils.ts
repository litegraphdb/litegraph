export const formatBytes = (bytes: number) => {
  if (bytes === 0) return '0 Bytes';
  const k = 1024;
  const sizes = ['Bytes', 'KB', 'MB', 'GB', 'TB', 'PB', 'EB', 'ZB', 'YB'];
  const i = Math.floor(Math.log(bytes) / Math.log(k));
  return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
};

/**
 * Triggers a browser download of a base64-encoded file.
 *
 * @param base64Data - The base64 encoded string (without data URI prefix).
 * @param filename - The desired filename for the downloaded file.
 * @param mimeType - Optional MIME type, defaults to 'application/octet-stream'.
 */
export function downloadBase64File(
  base64Data: string,
  filename: string,
  mimeType: string = 'application/octet-stream'
) {
  if (!base64Data) {
    console.error('No base64 data provided for download.');
    return;
  }

  try {
    const byteCharacters = atob(base64Data);
    const byteNumbers = new Array(byteCharacters.length);

    for (let i = 0; i < byteCharacters.length; i++) {
      byteNumbers[i] = byteCharacters.charCodeAt(i);
    }

    const byteArray = new Uint8Array(byteNumbers);

    const blob = new Blob([byteArray], { type: mimeType });

    const link = document.createElement('a');
    link.href = URL.createObjectURL(blob);
    link.download = filename || 'backup.bin';

    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);

    setTimeout(() => URL.revokeObjectURL(link.href), 100);
  } catch (error) {
    console.error('Download failed:', error);
  }
}

export const calculateTooltipPosition = (
  x: number,
  y: number,
  popupHeight: number = 320,
  popupWidth: number = 410
) => {
  // Get popup dimensions
  const viewportWidth = window.innerWidth;
  const viewportHeight = window.innerHeight;

  // Adjust X and Y to prevent overflow
  let posX = x;
  let posY = y;

  if (x + popupWidth > viewportWidth) {
    posX = viewportWidth - popupWidth - 10; // 10px margin
  }

  if (y + popupHeight > viewportHeight) {
    posY = viewportHeight - popupHeight - 10;
  }

  // Return final position
  return { x: posX, y: posY };
};

export const getCreateEditViewModelTitle = (
  suffix: string,
  isLoading: boolean,
  isCreate?: boolean,
  isEdit?: boolean,
  isView?: boolean
) => {
  if (isLoading) {
    return `Loading ${suffix}...`;
  }
  if (isView) {
    return `View ${suffix}`;
  }
  if (isCreate) {
    return `Create ${suffix}`;
  }
  if (isEdit) {
    return `Edit ${suffix}`;
  }
  return `${suffix}`;
};

export const decodeToJSON = (jsonString?: string) => {
  if (!jsonString) return null;

  try {
    return JSON.parse(jsonString);
  } catch (error: any) {
    console.error('Invalid JSON:', error.message);
    return null;
  }
};
