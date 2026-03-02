import toast from 'react-hot-toast';

const fallbackCopyText = (text: string): boolean => {
  const textarea = document.createElement('textarea');
  textarea.value = text;
  textarea.style.position = 'fixed';
  textarea.style.left = '-9999px';
  textarea.style.top = '-9999px';
  document.body.appendChild(textarea);
  textarea.focus();
  textarea.select();
  let success = false;
  try {
    success = document.execCommand('copy');
  } catch {
    success = false;
  }
  document.body.removeChild(textarea);
  return success;
};

export const copyTextToClipboard = async (text: string, label: string = 'Text', showToast: boolean = true): Promise<boolean> => {
  try {
    if (navigator.clipboard && window.isSecureContext) {
      await navigator.clipboard.writeText(text);
    } else {
      const ok = fallbackCopyText(text);
      if (!ok) throw new Error('execCommand copy failed');
    }
    if (showToast) toast.success(`${label} copied to clipboard`);
    return true;
  } catch (error) {
    console.error('Copy failed:', error);
    if (showToast) toast.error(`Failed to copy ${label}`);
    return false;
  }
};

export const copyJsonToClipboard = async (data: any, label: string = 'JSON', showToast: boolean = true): Promise<boolean> => {
  try {
    const jsonString = JSON.stringify(data, null, 2);
    return await copyTextToClipboard(jsonString, label, showToast);
  } catch (error) {
    console.error('Copy failed:', error);
    if (showToast) toast.error(`Failed to copy ${label}`);
    return false;
  }
};
