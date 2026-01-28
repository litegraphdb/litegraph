import toast from 'react-hot-toast';

export const copyTextToClipboard = (text: string, label: string = 'Text') => {
  try {
    navigator.clipboard.writeText(text);
    toast.success(`${label} copied to clipboard`);
  } catch (error) {
    console.error('Copy failed:', error);
    toast.error(`Failed to copy ${label}`);
  }
};

export const copyJsonToClipboard = (data: any, label: string = 'JSON') => {
  try {
    const jsonString = JSON.stringify(data, null, 2);
    copyTextToClipboard(jsonString, label);
  } catch (error) {
    console.error('Copy failed:', error);
    toast.error(`Failed to copy ${label}`);
  }
};
