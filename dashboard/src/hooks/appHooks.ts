import { tablePaginationConfig } from '@/constants/pagination';
import { createContext, useContext, useEffect, useState } from 'react';
import { ThemeEnum } from '@/types/types';
import { url } from 'inspector';

export const usePagination = () => {
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(tablePaginationConfig.pageSize);
  const [skip, setSkip] = useState(0);

  const handlePageChange = (page: number, pageSize: number) => {
    setPage(page);
    setPageSize(pageSize);
    setSkip((page - 1) * pageSize);
  };

  return { page, pageSize, skip, handlePageChange };
};

export const AppContext = createContext({
  theme: ThemeEnum.LIGHT,
  setTheme: (theme: ThemeEnum) => {},
});

export const useAppContext = () => {
  return useContext(AppContext);
};

export const useCurrentlyHostedDomainAsServerUrl = () => {
  const [serverUrl, setServerUrl] = useState<string>('');
  useEffect(() => {
    const currentlyHostedDomain = window.location.origin;
    const url = new URL(currentlyHostedDomain);
    url.port = '8701';
    setServerUrl(url.toString());
  }, []);
  return serverUrl || '';
};
