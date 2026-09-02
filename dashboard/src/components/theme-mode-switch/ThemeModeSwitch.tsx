import React from 'react';
import { useAppContext } from '@/hooks/appHooks';
import { ThemeEnum } from '@/types/types';

const SIZE = 20;
const TRANSITION = 'all 0.45s ease';

const ThemeModeSwitch = () => {
  const { theme, setTheme } = useAppContext();
  const isDark = theme === ThemeEnum.DARK;

  return (
    <button
      type="button"
      role="switch"
      aria-checked={isDark}
      aria-label="Toggle dark mode"
      onClick={() => setTheme(isDark ? ThemeEnum.LIGHT : ThemeEnum.DARK)}
      style={{
        background: 'none',
        border: 'none',
        padding: 0,
        cursor: 'pointer',
        display: 'inline-flex',
        alignItems: 'center',
        color: 'inherit',
      }}
    >
      <svg
        width={SIZE}
        height={SIZE}
        viewBox="0 0 24 24"
        style={{
          transition: TRANSITION,
          transform: isDark ? 'rotate(40deg)' : 'rotate(90deg)',
        }}
      >
        <mask id="theme-mode-switch-moon-mask">
          <rect x="0" y="0" width="100%" height="100%" fill="white" />
          <circle
            r="9"
            fill="black"
            style={
              {
                transition: TRANSITION,
                cx: isDark ? '17px' : '26px',
                cy: isDark ? '7px' : '-2px',
              } as React.CSSProperties
            }
          />
        </mask>
        <circle
          cx="12"
          cy="12"
          fill="currentColor"
          mask="url(#theme-mode-switch-moon-mask)"
          style={{ transition: TRANSITION, r: isDark ? '9px' : '5px' } as React.CSSProperties}
        />
        <g
          stroke="currentColor"
          strokeWidth="2"
          strokeLinecap="round"
          style={{ transition: TRANSITION, opacity: isDark ? 0 : 1 }}
        >
          <line x1="12" y1="1" x2="12" y2="3" />
          <line x1="12" y1="21" x2="12" y2="23" />
          <line x1="4.22" y1="4.22" x2="5.64" y2="5.64" />
          <line x1="18.36" y1="18.36" x2="19.78" y2="19.78" />
          <line x1="1" y1="12" x2="3" y2="12" />
          <line x1="21" y1="12" x2="23" y2="12" />
          <line x1="4.22" y1="19.78" x2="5.64" y2="18.36" />
          <line x1="18.36" y1="5.64" x2="19.78" y2="4.22" />
        </g>
      </svg>
    </button>
  );
};

export default ThemeModeSwitch;
