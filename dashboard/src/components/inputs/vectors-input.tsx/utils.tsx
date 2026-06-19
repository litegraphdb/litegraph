export const parseVectorValuesInput = (
  value: unknown
): { values: number[]; valid: boolean } => {
  let rawValues: unknown[];

  if (Array.isArray(value)) {
    rawValues = value;
  } else if (typeof value === 'string') {
    const trimmed = value.trim();
    if (!trimmed) {
      return { values: [], valid: false };
    }

    if (trimmed.startsWith('[')) {
      try {
        const parsed = JSON.parse(trimmed);
        rawValues = Array.isArray(parsed) ? parsed : [];
      } catch {
        return { values: [], valid: false };
      }
    } else {
      rawValues = trimmed.split(/[\s,]+/).filter(Boolean);
    }
  } else {
    return { values: [], valid: false };
  }

  const values = rawValues.map((vectorValue) => Number(vectorValue));
  const valid = rawValues.length > 0 && values.every((vectorValue) => Number.isFinite(vectorValue));

  return { values: valid ? values : [], valid };
};

export const vectorValuesToInputText = (value: unknown): string => {
  if (typeof value === 'string') {
    return value;
  }

  const parsed = parseVectorValuesInput(value);
  if (parsed.valid) {
    return `[${parsed.values.join(', ')}]`;
  }

  if (Array.isArray(value) && value.length > 0) {
    return JSON.stringify(value);
  }

  return '';
};

export const convertVectorsToAPIRecord = (
  vectors?: Array<{ Model: string; Dimensionality: number; Content: string; Vectors: unknown }>
) => {
  return (
    vectors?.map((vector: any) => {
      const parsedVectorValues = parseVectorValuesInput(vector.Vectors);

      return {
        ...vector,
        Dimensionality: Number(vector.Dimensionality),
        Vectors: parsedVectorValues.valid ? parsedVectorValues.values : [],
      };
    }) || []
  );
};
