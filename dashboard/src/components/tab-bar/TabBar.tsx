'use client';
import React from 'react';
import { Tabs } from 'antd';
import { useRouter } from 'next/navigation';
import styles from './tabBar.module.scss';

/** One tab: `key` identifies it, `href` is where activating it navigates. */
export interface TabBarItem {
  key: string;
  label: React.ReactNode;
  href: string;
  title?: string;
}

interface TabBarProps {
  items: TabBarItem[];
  /** Key of the active tab; null or undefined when no tab is active (for example a bare hub path). */
  activeKey?: string | null;
  /** Accessible name for the tab list. */
  ariaLabel: string;
  /** Optional content at the right of the bar, such as a scope selector. Wraps below the tabs on narrow screens. */
  rightSlot?: React.ReactNode;
}

/**
 * The shared, path-based tab bar used by every hub layout. Each tab is a URL: activating a tab (click, or arrow keys
 * and Enter through antd's tab list) navigates to its `href`, so tabs can be bookmarked and shared. Tabs that do not
 * fit scroll, with an overflow menu, and the right slot wraps below the tabs on narrow screens.
 */
const TabBar = ({ items, activeKey, ariaLabel, rightSlot }: TabBarProps) => {
  const router = useRouter();

  const handleChange = (key: string) => {
    const item = items.find((entry) => entry.key === key);
    if (item && key !== activeKey) router.push(item.href);
  };

  return (
    <nav className={styles.tabBar} aria-label={ariaLabel} data-testid="hub-tab-bar">
      <Tabs
        className={styles.tabs}
        activeKey={activeKey ?? ''}
        onChange={handleChange}
        items={items.map((item) => ({
          key: item.key,
          label: (
            <span title={item.title} data-testid={`hub-tab-${item.key}`}>
              {item.label}
            </span>
          ),
        }))}
      />
      {rightSlot && (
        <div className={styles.rightSlot} data-testid="hub-tab-bar-right">
          {rightSlot}
        </div>
      )}
    </nav>
  );
};

export default TabBar;
