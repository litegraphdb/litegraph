const defaultLiteGraphInstanceURL = 'http://localhost:8701/';

const normalizeUrl = (value?: string) => {
  const trimmed = value?.trim();
  if (!trimmed) {
    return '';
  }

  return trimmed.endsWith('/') ? trimmed : `${trimmed}/`;
};

export const configuredLiteGraphInstanceURL = normalizeUrl(
  process.env.NEXT_PUBLIC_LITEGRAPH_SERVER_URL || process.env.LITEGRAPH_SERVER
);

export const liteGraphInstanceURL = configuredLiteGraphInstanceURL || defaultLiteGraphInstanceURL;

export const globalToastId = 'global-toast';
