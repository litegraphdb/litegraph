'use client';
import { useEffect, useState } from 'react';
import { Alert } from 'antd';
import { useTranslations } from 'next-intl';
import { usePrincipal } from '@/hooks/permissionHooks';
import { getBaseUrl, request } from '@/lib/sdk/settings';

export const defaultCredentialsDismissalKey = 'litegraph-default-credentials-dismissed';

interface DefaultCredentialStatus {
  HasDefaultCredentials: boolean;
}

/** Dismissal lasts for this login, across dashboard routes and reloads. */
const DefaultCredentialsBanner = () => {
  const principal = usePrincipal();
  const isAdmin = principal.isSystemAdmin || principal.isBreakGlass;
  const t = useTranslations('defaultCredentials');
  const [visible, setVisible] = useState(false);

  useEffect(() => {
    let cancelled = false;
    setVisible(false);
    if (!isAdmin || sessionStorage.getItem(defaultCredentialsDismissalKey)) return;
    request<DefaultCredentialStatus>('GET', `${getBaseUrl()}/v1.0/security/default-credentials`)
      .then((status) => {
        if (!cancelled) setVisible(status.HasDefaultCredentials);
      })
      .catch(() => {
        // A failed diagnostic must not prevent use of the dashboard.
      });
    return () => { cancelled = true; };
  }, [isAdmin]);

  if (!visible) return null;
  return (
    <Alert
      type="warning"
      showIcon
      closable
      message={t('title')}
      description={t('description')}
      style={{ marginBottom: 16 }}
      onClose={() => {
        sessionStorage.setItem(defaultCredentialsDismissalKey, 'true');
        setVisible(false);
      }}
    />
  );
};

export default DefaultCredentialsBanner;
