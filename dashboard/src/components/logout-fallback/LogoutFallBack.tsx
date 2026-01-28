import React, { useEffect, useState } from 'react';
import { useLogout } from '@/hooks/authHooks';
import FallBack from '@/components/base/fallback/FallBack';

const LogoutFallBack = ({ message, logoutPath }: { message?: string; logoutPath?: string }) => {
  const [countdown, setCountdown] = useState(3);
  const logout = useLogout();

  useEffect(() => {
    const timer = setInterval(() => {
      if (countdown > 0) {
        setCountdown((prev) => prev - 1);
      } else {
        logout(logoutPath);
      }
    }, 1000);

    return () => clearInterval(timer);
  }, [logout, countdown]);

  return (
    <FallBack className="mt-12 pt-12">
      {message || 'Session invalid.'} Logging out in {countdown} seconds...
    </FallBack>
  );
};

export default LogoutFallBack;
