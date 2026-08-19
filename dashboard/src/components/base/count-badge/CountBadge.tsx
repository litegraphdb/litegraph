import React from 'react';
import LitegraphTag from '@/components/base/tag/Tag';

type CountBadgeProps = {
  count?: number;
  /** English noun used for the default (non-localized) pluralization. */
  noun?: string;
  /** Pre-formatted, localized label. Overrides `noun`-based formatting. */
  label?: string;
};

const CountBadge = ({ count = 0, noun = '', label: labelOverride }: CountBadgeProps) => {
  const normalizedCount = Number.isFinite(count) && count > 0 ? count : 0;
  const label =
    labelOverride ?? `${normalizedCount} ${noun}${normalizedCount === 1 ? '' : 's'}`;

  return <LitegraphTag label={label} data-testid="count-badge" />;
};

export default CountBadge;
