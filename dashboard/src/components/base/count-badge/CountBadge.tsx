import React from 'react';
import LitegraphTag from '@/components/base/tag/Tag';

type CountBadgeProps = {
  count?: number;
  noun: string;
};

const CountBadge = ({ count = 0, noun }: CountBadgeProps) => {
  const normalizedCount = Number.isFinite(count) && count > 0 ? count : 0;
  const label = `${normalizedCount} ${noun}${normalizedCount === 1 ? '' : 's'}`;

  return <LitegraphTag label={label} data-testid="count-badge" />;
};

export default CountBadge;
