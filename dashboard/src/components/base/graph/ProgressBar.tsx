import { getPercentage, humanizeNumber } from '@/utils/dataUtils';
import { Progress } from 'antd';
import React from 'react';
import styles from './graph.module.scss';
import { LightGraphTheme } from '@/theme/theme';

const ProgressBar = ({
  loaded,
  total,
  label,
}: {
  loaded: number;
  total: number;
  label: string;
}) => {
  return (
    <div className={styles.progressContainer}>
      <span className="text-white-space-nowrap">
        {label}({humanizeNumber(loaded)}/{humanizeNumber(total) || '...'})
      </span>
      <Progress
        strokeColor={LightGraphTheme.primary}
        percent={getPercentage(loaded, total)}
        showInfo={false}
      />
    </div>
  );
};

export default ProgressBar;
