'use client';

import { useEffect, useState } from 'react';
import { useSearchParams } from 'next/navigation';
import { useApiKeyToLogin } from '@/hooks/authHooks';
import { paths } from '@/constants/constant';
import { dynamicSlugs } from '@/constants/constant';
import FallBack from '@/components/base/fallback/FallBack';
import LitegraphFlex from '@/components/base/flex/Flex';
import PageLoading from '@/components/base/loading/PageLoading';
import PageContainer from '@/components/base/pageContainer/PageContainer';
import LitegraphText from '@/components/base/typograpghy/Text';
import LitegraphTitle from '@/components/base/typograpghy/Title';

export default function Home() {
  const [isLoading, setIsLoading] = useState(false);
  const searchParams = useSearchParams();
  const apikey = searchParams?.get('apikey');
  const endpoint = searchParams?.get('endpoint');
  const { loginWithApiKey } = useApiKeyToLogin();
  const [isLoggedIn, setIsLoggedIn] = useState(false);

  const handleLogin = async (apikey: string, endpoint: string) => {
    setIsLoading(true);
    try {
      const result = await loginWithApiKey(apikey, endpoint);
      setIsLoading(false);
      if (result.success && result.tenant) {
        setIsLoggedIn(true);

        // Wait a bit longer to ensure Redux state is updated
        setTimeout(() => {
          // Use tenant GUID from the login response
          const currentTenantGUID = result.tenant.GUID;

          if (!currentTenantGUID) {
            console.error('No tenant GUID available for redirect');
            return;
          }

          // Manually construct the dashboard path
          const dashboardPath = paths.dashboardHome.replace(
            dynamicSlugs.tenantId,
            currentTenantGUID
          );

          // Use window.location.href instead of router.push for more reliable redirect
          window.location.href = dashboardPath;
        }, 2000);
      }
    } catch (error) {
      console.error('SSO login error:', error);
      setIsLoading(false);
    }
  };

  useEffect(() => {
    if (apikey && endpoint) {
      handleLogin(apikey, endpoint);
    }
  }, [apikey, endpoint]);

  return (
    <PageContainer withoutWhiteBG>
      <LitegraphFlex vertical justify="center" align="center">
        <LitegraphTitle level={3} weight={600}>
          SSO Login
        </LitegraphTitle>
        {!apikey || !endpoint ? (
          <FallBack>Missing API Key or Endpoint.</FallBack>
        ) : isLoading ? (
          <PageLoading message={<LitegraphTitle level={4}>Logging in...</LitegraphTitle>} />
        ) : isLoggedIn ? (
          <LitegraphText>Redirecting to dashboard...</LitegraphText>
        ) : (
          <FallBack retry={() => handleLogin(apikey!, endpoint!)}>
            Cannot validate the API key.
          </FallBack>
        )}
      </LitegraphFlex>
    </PageContainer>
  );
}
