export const convertTagsToRecord = (tags: Array<{ key: string; value: string }>) => {
  const record: Record<string, string> = {};
  tags.forEach((tag: { key: string; value: string }) => {
    record[tag.key] = tag.value;
  });
  return record;
};
