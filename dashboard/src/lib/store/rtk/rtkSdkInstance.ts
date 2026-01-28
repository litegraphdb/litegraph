import { createApi } from '@reduxjs/toolkit/query/react';

import { BaseQueryFn } from '@reduxjs/toolkit/query';
import { keepUnusedDataFor } from '@/constants/constant';

export interface SdkBaseQueryArgs {
  callback: () => Promise<any>;
}

export const sdkBaseQuery: BaseQueryFn<SdkBaseQueryArgs, unknown, unknown> = async ({
  callback,
}: {
  callback: <T>() => Promise<T>;
}) => {
  try {
    const result = await callback();
    return { data: result };
  } catch (error) {
    console.error('RTK SDK Base Query Error:', error);

    // Log the full error object for debugging
    console.log('Full error object:', JSON.stringify(error, null, 2));

    return { error };
  }
};

const sdkSlice = createApi({
  reducerPath: 'sdk',
  baseQuery: sdkBaseQuery,
  tagTypes: [],
  endpoints: () => ({}),
  keepUnusedDataFor: keepUnusedDataFor,
});

export default sdkSlice;
