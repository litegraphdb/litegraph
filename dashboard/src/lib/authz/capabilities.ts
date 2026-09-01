/**
 * Declarative client-side authorization capability map.
 *
 * This is the SINGLE source of truth for UX permission gating. It is consumed
 * by (1) navigation rendering, (2) route guards, and (3) per-control
 * disabled/hidden state so the sidebar, the page, and the buttons can never
 * disagree. It intentionally MIRRORS the server-enforced rules; the server
 * remains the authority. Client gating is UX only.
 *
 * Roles:
 *  - SystemAdmin: superuser across every tenant and every operation.
 *  - TenantAdmin: full rights within their own tenant only.
 *  - Regular user: read-only view of their own tenant; under SECURE may view
 *    and edit ONLY their own user record and their own credentials.
 *  - Break-glass: the static admin bearer token; treated as SystemAdmin.
 */

export type CapabilityAction = 'view' | 'edit';

export type CapabilityResource =
  | 'home'
  | 'graphs'
  | 'nodes'
  | 'edges'
  | 'labels'
  | 'tags'
  | 'vectors'
  | 'requests'
  | 'apiExplorer'
  | 'aiChat'
  | 'aiEndpoints'
  | 'aiHistory'
  | 'aiFeedback'
  | 'aiSettings'
  | 'tenants'
  | 'users'
  | 'credentials'
  | 'authorization'
  | 'backups'
  | 'settings';

export type NavSectionId =
  | 'home'
  | 'data'
  | 'metadata'
  | 'ai'
  | 'manage'
  | 'secure'
  | 'administer';

/** The authenticated principal, derived from the session (see usePrincipal). */
export interface Principal {
  /** Server-wide superuser. */
  isSystemAdmin: boolean;
  /** Full rights within the principal's own tenant. */
  isTenantAdmin: boolean;
  /** Break-glass admin bearer token session; treated as SystemAdmin. */
  isBreakGlass: boolean;
  /** The signed-in user's GUID, when a real user session exists. */
  userGuid: string | null;
  /** The principal's own tenant GUID. */
  tenantGuid: string | null;
}

/** Optional narrowing for record-level checks (own tenant / own record). */
export interface CapabilityScope {
  /** The tenant the target resource belongs to. */
  tenantGuid?: string | null;
  /** The user that owns the target record (for self-service checks). */
  ownerUserGuid?: string | null;
}

const isSuper = (p: Principal): boolean => p.isSystemAdmin || p.isBreakGlass;

const sameTenant = (p: Principal, scope?: CapabilityScope): boolean => {
  if (!scope || scope.tenantGuid == null) return true;
  return !!p.tenantGuid && p.tenantGuid === scope.tenantGuid;
};

const ownsRecord = (p: Principal, scope?: CapabilityScope): boolean => {
  if (!scope || scope.ownerUserGuid == null) return true;
  return !!p.userGuid && p.userGuid === scope.ownerUserGuid;
};

/**
 * Answer "can this principal perform `action` on `resource` (optionally within
 * `scope`)?". Returns a boolean; never throws.
 */
export const can = (
  principal: Principal | null | undefined,
  action: CapabilityAction,
  resource: CapabilityResource,
  scope?: CapabilityScope
): boolean => {
  if (!principal) return false;
  if (isSuper(principal)) return true;

  const tenantAdmin = principal.isTenantAdmin;

  switch (resource) {
    // Tenant-scoped operational surfaces (DATA / METADATA / MANAGE).
    case 'home':
      return true;
    case 'graphs':
    case 'nodes':
    case 'edges':
    case 'labels':
    case 'tags':
    case 'vectors':
      if (action === 'view') return true;
      // Only tenant-admins may edit tenant data; regular users are read-only.
      return tenantAdmin && sameTenant(principal, scope);
    case 'requests':
      return action === 'view';
    case 'apiExplorer':
      // Everyone may open the explorer; execution is still server-authorized.
      return true;

    // AI — chat is open to every tenant principal; administration of
    // endpoints, history, feedback, and chat settings is tenant-admin only.
    case 'aiChat':
      return true;
    case 'aiEndpoints':
    case 'aiHistory':
    case 'aiFeedback':
    case 'aiSettings':
      return tenantAdmin && sameTenant(principal, scope);

    // SECURE — server-level, permission-filtered.
    case 'tenants':
      if (action === 'view') return true; // regular users see their tenant read-only
      return tenantAdmin && sameTenant(principal, scope);
    case 'users':
    case 'credentials':
      if (tenantAdmin) return sameTenant(principal, scope);
      // Regular users may view and edit ONLY their own record.
      return ownsRecord(principal, scope);
    case 'authorization':
      return tenantAdmin && sameTenant(principal, scope);

    // ADMINISTER — SystemAdmin only (already returned true above).
    case 'backups':
    case 'settings':
      return false;

    default:
      return false;
  }
};

/**
 * Whether a nav section should be rendered for the principal. A section is
 * visible when at least one of its resources is viewable.
 */
export const canViewSection = (
  principal: Principal | null | undefined,
  section: NavSectionId
): boolean => {
  if (!principal) return false;
  switch (section) {
    case 'home':
      return true;
    case 'data':
      return can(principal, 'view', 'graphs');
    case 'metadata':
      return can(principal, 'view', 'labels');
    case 'ai':
      return (
        can(principal, 'view', 'aiChat') ||
        can(principal, 'view', 'aiEndpoints') ||
        can(principal, 'view', 'aiHistory') ||
        can(principal, 'view', 'aiFeedback') ||
        can(principal, 'view', 'aiSettings')
      );
    case 'manage':
      return can(principal, 'view', 'requests') || can(principal, 'view', 'apiExplorer');
    case 'secure':
      return (
        can(principal, 'view', 'tenants') ||
        can(principal, 'view', 'users') ||
        can(principal, 'view', 'credentials') ||
        can(principal, 'view', 'authorization')
      );
    case 'administer':
      return can(principal, 'view', 'backups') || can(principal, 'view', 'settings');
    default:
      return false;
  }
};
