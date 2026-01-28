export const convertVectorsToAPIRecord = (
  vectors?: Array<{ Model: string; Dimensionality: number; Content: string; Vectors: number[] }>
) => {
  return (
    vectors?.map((vector: any) => ({
      ...vector,
      Dimensionality: Number(vector.Dimensionality),
      Vectors: vector.Vectors || [],
    })) || []
  );
};
