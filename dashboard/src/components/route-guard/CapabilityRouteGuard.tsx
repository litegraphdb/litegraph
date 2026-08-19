'use client';
import React from 'react';
import { useTranslations } from 'next-intl';
import { Result } from 'antd';
import { useCan } from '@/hooks/permissionHooks';
import { CapabilityResource } from '@/lib/authz/capabilities';
import LitegraphButton from '@/components/base/button/Button';
import { useAppDynamicNavigation } from '@/hooks/hooks';
import { paths } from '@/constants/constant';

interface CapabilityRouteGuardProps {
  resource: CapabilityResource;
  children: React.ReactNode;
}

/**
 * Client-side route guard driven by the capability map. When the principal may
 * not view `resource`, it renders a localized "access denied" state instead of
 * the page. This mirrors the server's authorization (which remains the
 * authority); it exists so a user who deep-links to a forbidden URL is denied
 * client-side rather than seeing a broken page.
 */
const CapabilityRouteGuard = ({ resource, children }: CapabilityRouteGuardProps) => {
  const t = useTranslations('secure');
  const { can } = useCan();
  const { navigate } = useAppDynamicNavigation();

  if (!can('view', resource)) {
    return (
      <div data-testid="capability-denied" style={{ padding: 24 }}>
        <Result
          status="403"
          title={t('denied.title')}
          subTitle={t('denied.subtitle')}
          extra={
            <LitegraphButton type="primary" onClick={() => navigate(paths.dashboardHome)}>
              {t('denied.backHome')}
            </LitegraphButton>
          }
        />
      </div>
    );
  }

  return <>{children}</>;
};

export default CapabilityRouteGuard;
