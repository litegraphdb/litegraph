export function onTagFilter(val: any, record: { [key: string]: any }) {
  return Object.entries(record).some(([key, value]) => {
    const valString = val?.toString()?.toLowerCase() || '';
    return (
      key?.toLowerCase()?.includes(valString) ||
      value?.toString()?.toLowerCase()?.includes(valString)
    );
  });
}

export function onLabelFilter(val: any, record: string[]) {
  console.log(val, record);
  return record.some((label: string) => label.toLowerCase().includes(val?.toLowerCase() as string));
}

export function onGUIDFilter(val: any, record: string) {
  return record.toLowerCase().includes(val.toString().toLowerCase());
}

export function onNameFilter(val: any, record: string) {
  return record.toLowerCase().includes(val.toString().toLowerCase());
}
