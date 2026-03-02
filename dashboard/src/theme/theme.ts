import { theme, ThemeConfig } from 'antd';

export const LightGraphTheme = {
  primary: '#4a8c3f',
  primaryLight: '#6cc955',
  primaryLight2: '#e8f5e3',
  primaryRed: '#ef4444',
  secondaryBlue: '#dbeafe',
  secondaryYellow: '#fef3c7',
  borderGray: '#e2e8f0',
  borderGrayDark: '#333333',
  white: '#ffffff',
  fontFamily: '"Inter", sans-serif',
  colorBgContainerDisabled: '#f1f5f9',
  colorBgContainerDisabledDark: '#2a2a2a',
  textDisabled: '#94a3b8',
  subHeadingColor: '#64748b',
  bgSecondary: '#f8fafc',
  bgTertiary: '#f1f5f9',
  darkBgBase: '#141414',
  darkBgElevated: '#1f1f1f',
  darkBgTertiary: '#2a2a2a',
};

export const primaryTheme: ThemeConfig = {
  cssVar: true,
  algorithm: theme.defaultAlgorithm,
  token: {
    colorPrimary: LightGraphTheme.primary,
    fontFamily: LightGraphTheme.fontFamily,
    colorBorder: LightGraphTheme.borderGray,
    colorTextDisabled: LightGraphTheme.textDisabled,
    colorBgContainerDisabled: LightGraphTheme.colorBgContainerDisabled,
    borderRadius: 8,
    borderRadiusLG: 12,
    borderRadiusSM: 6,
    colorBgLayout: '#f8fafc',
    controlHeight: 36,
  },
  components: {
    Tabs: {
      cardBg: '#f1f5f9',
      titleFontSize: 13,
    },
    Typography: {
      fontWeightStrong: 600,
    },
    Layout: {
      fontFamily: LightGraphTheme.fontFamily,
    },
    Menu: {
      itemSelectedBg: LightGraphTheme.primaryLight2,
      itemBorderRadius: 8,
      itemHoverBg: '#f1f5f9',
      itemMarginInline: 8,
    },
    Button: {
      borderRadius: 8,
      primaryColor: LightGraphTheme.white,
      defaultColor: '#334155',
      colorLink: LightGraphTheme.primary,
      colorLinkHover: LightGraphTheme.primary,
      primaryShadow: '0 2px 4px rgba(74, 140, 63, 0.2)',
    },
    Table: {
      headerBg: '#f1f5f9',
      headerColor: '#475569',
      rowHoverBg: '#f8fafc',
      headerBorderRadius: 8,
      padding: 16,
      borderColor: '#d1d5db',
    },
    Collapse: {
      headerBg: '#f1f5f9',
    },
    Input: {
      borderRadius: 6,
      borderRadiusLG: 8,
      borderRadiusXS: 3,
      activeShadow: '0 0 0 3px rgba(74, 140, 63, 0.1)',
    },
    Select: {
      borderRadius: 6,
      borderRadiusLG: 8,
      borderRadiusXS: 3,
      optionSelectedColor: LightGraphTheme.white,
      optionSelectedBg: LightGraphTheme.primary,
      optionActiveBg: '#e8f5e3',
    },
    Pagination: {
      fontFamily: LightGraphTheme.fontFamily,
      borderRadius: 6,
    },
    Form: {
      labelColor: LightGraphTheme.subHeadingColor,
      colorBorder: 'none',
      verticalLabelPadding: 0,
      itemMarginBottom: 12,
    },
    Modal: {
      borderRadiusLG: 12,
      titleFontSize: 18,
    },
    Card: {
      borderRadiusLG: 12,
    },
    Tooltip: {
      borderRadius: 6,
      colorBgSpotlight: '#1e293b',
    },
  },
};

export const darkTheme: ThemeConfig = {
  cssVar: true,
  algorithm: theme.darkAlgorithm,
  token: {
    colorBgBase: '#141414',
    colorPrimary: LightGraphTheme.primaryLight,
    fontFamily: LightGraphTheme.fontFamily,
    colorBorder: LightGraphTheme.borderGrayDark,
    colorTextDisabled: LightGraphTheme.textDisabled,
    colorBgContainerDisabled: LightGraphTheme.colorBgContainerDisabledDark,
    borderRadius: 8,
    borderRadiusLG: 12,
    borderRadiusSM: 6,
    colorBgLayout: '#1f1f1f',
    controlHeight: 36,
  },
  components: {
    Tabs: {
      cardBg: '#1f1f1f',
      titleFontSize: 13,
    },
    Typography: {
      fontWeightStrong: 600,
    },
    Layout: {
      fontFamily: LightGraphTheme.fontFamily,
    },
    Menu: {
      itemSelectedBg: '#1f1f1f',
      itemSelectedColor: 'var(--ant-color-primary)',
      itemBorderRadius: 8,
      itemHoverBg: '#2a2a2a',
      itemMarginInline: 8,
    },
    Button: {
      borderRadius: 8,
      primaryColor: LightGraphTheme.white,
      defaultColor: 'var(--ant-color-text-base)',
      colorLink: LightGraphTheme.primaryLight,
      colorLinkHover: LightGraphTheme.primaryLight,
      primaryShadow: '0 2px 4px rgba(0, 0, 0, 0.3)',
    },
    Table: {
      headerBg: '#1f1f1f',
      headerColor: '#a3a3a3',
      rowHoverBg: '#2a2a2a',
      headerBorderRadius: 8,
      padding: 16,
      borderColor: 'var(--ant-color-border)',
    },
    Collapse: {
      headerBg: '#1f1f1f',
    },
    Input: {
      borderRadius: 6,
      borderRadiusLG: 8,
      borderRadiusXS: 3,
      activeShadow: '0 0 0 3px rgba(74, 140, 63, 0.1)',
    },
    Select: {
      borderRadius: 6,
      borderRadiusLG: 8,
      borderRadiusXS: 3,
      optionSelectedColor: LightGraphTheme.white,
      optionSelectedBg: LightGraphTheme.primary,
      optionActiveBg: '#2a2a2a',
    },
    Pagination: {
      fontFamily: LightGraphTheme.fontFamily,
      borderRadius: 6,
    },
    Form: {
      labelColor: 'var(--ant-color-text-base)',
      colorBorder: 'none',
      verticalLabelPadding: 0,
      itemMarginBottom: 12,
    },
    Modal: {
      borderRadiusLG: 12,
      titleFontSize: 18,
    },
    Card: {
      borderRadiusLG: 12,
    },
    Tooltip: {
      borderRadius: 6,
      colorBgSpotlight: '#1f1f1f',
    },
  },
};
