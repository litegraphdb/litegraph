import React from 'react';
import LitegraphTooltip from '@/components/base/tooltip/Tooltip';

export const columnTooltip = (label: string, description: string) => (
  <LitegraphTooltip title={description}>
    <span>{label}</span>
  </LitegraphTooltip>
);
