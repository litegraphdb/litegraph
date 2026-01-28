import { useRouter } from 'next/navigation';
import { useCurrentTenant } from './entityHooks';
import toast from 'react-hot-toast';
import { NavigateOptions } from 'next/dist/shared/lib/app-router-context.shared-runtime';
import { dynamicSlugs } from '@/constants/constant';
import { globalToastId } from '@/constants/config';

export const useAppDynamicNavigation = () => {
  const router = useRouter();
  const tenant = useCurrentTenant();

  const serializePath = (path?: string) => {
    return path ? path.replace(dynamicSlugs.tenantId, tenant?.GUID || '') : '';
  };
  const navigate = (href: string, options?: NavigateOptions) => {
    let updatedHref = href;
    if (updatedHref.includes(dynamicSlugs.tenantId)) {
      if (tenant?.GUID) {
        updatedHref = serializePath(updatedHref);
        router.push(updatedHref, options);
      } else {
        toast.error('Error while navigating', { id: globalToastId });
        //eslint-disable-next-line no-console
        console.log('No tenant found, cannot navigate.');
      }
    } else {
      router.push(href, options);
    }
  };
  return { navigate, serializePath };
};
