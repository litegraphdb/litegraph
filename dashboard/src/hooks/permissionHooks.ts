import { useMemo } from 'react';
import { useAppSelector } from '@/lib/store/hooks';
import { RootState } from '@/lib/store/store';
import { FlaggedUser } from '@/types/types';
import {
  CapabilityAction,
  CapabilityResource,
  CapabilityScope,
  NavSectionId,
  Principal,
  can,
  canViewSection,
} from '@/lib/authz/capabilities';

/**
 * Build the authenticated {@link Principal} from the current session. Capability
 * flags come from the signed-in user record; a break-glass admin bearer token
 * (no user record) is treated as a SystemAdmin-equivalent superuser.
 */
export const usePrincipal = (): Principal => {
  const user = useAppSelector((state: RootState) => state.liteGraph.user) as FlaggedUser | null;
  const tenant = useAppSelector((state: RootState) => state.liteGraph.tenant);
  const adminAccessKey = useAppSelector((state: RootState) => state.liteGraph.adminAccessKey);

  return useMemo<Principal>(
    () => ({
      isSystemAdmin: Boolean(user?.IsSystemAdmin),
      isTenantAdmin: Boolean(user?.IsTenantAdmin),
      isBreakGlass: Boolean(adminAccessKey),
      userGuid: user?.GUID ?? null,
      tenantGuid: user?.TenantGUID ?? tenant?.GUID ?? null,
    }),
    [user, tenant, adminAccessKey]
  );
};

/**
 * Convenience hook returning a bound `can(action, resource, scope?)` predicate
 * for the current principal, plus a `canViewSection` helper. Use this for nav
 * rendering, route guards, and per-control disabled/hidden state.
 */
export const useCan = () => {
  const principal = usePrincipal();
  return useMemo(
    () => ({
      principal,
      can: (action: CapabilityAction, resource: CapabilityResource, scope?: CapabilityScope) =>
        can(principal, action, resource, scope),
      canViewSection: (section: NavSectionId) => canViewSection(principal, section),
    }),
    [principal]
  );
};
