/** @type {import('next').NextConfig} */
const nextConfig = {
  // eslint: {
  //   ignoreDuringBuilds: true,
  // },
  // typescript: {
  //   ignoreBuildErrors: true,
  // },
  images: {
    unoptimized: true,
  },
  env: {
    PORT: '3000',
    LITEGRAPH_SERVER:
      process.env.LITEGRAPH_SERVER || process.env.NEXT_PUBLIC_LITEGRAPH_SERVER_URL || '',
    NEXT_PUBLIC_LITEGRAPH_SERVER_URL:
      process.env.NEXT_PUBLIC_LITEGRAPH_SERVER_URL || process.env.LITEGRAPH_SERVER || '',
  },
  reactStrictMode: false,
};

module.exports = nextConfig;
