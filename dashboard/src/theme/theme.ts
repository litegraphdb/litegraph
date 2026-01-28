import { theme, ThemeConfig } from 'antd';

export const LightGraphTheme = {
  primary: '#558f42', //95DB7B
  primaryLight: '#86d96a',
  primaryLight2: '#e4ffdb',
  primaryRed: '#d9383a',
  secondaryBlue: '#b1e5ff',
  secondaryYellow: '#ffe362',
  borderGray: '#C1C1C1',
  borderGrayDark: '#666666',
  white: '#ffffff',
  fontFamily: '"Inter", "serif"',
  colorBgContainerDisabled: '#E9E9E9',
  colorBgContainerDisabledDark: '#555555',
  textDisabled: '#bbbbbb',
  subHeadingColor: '#666666',
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
  },
  components: {
    Tabs: {
      cardBg: '#F2F2F2',
      titleFontSize: 12,
    },
    Typography: {
      fontWeightStrong: 400,
    },
    Layout: {
      fontFamily: LightGraphTheme.fontFamily,
    },
    Menu: {
      itemSelectedBg: LightGraphTheme.primaryLight2,
    },
    Button: {
      borderRadius: 4,
      primaryColor: LightGraphTheme.white,
      defaultColor: '#333333',
      colorLink: LightGraphTheme.primary,
      colorLinkHover: LightGraphTheme.primary,
    },
    Table: {
      headerBg: '#ffffff',
      padding: 18,
      borderColor: '#d1d5db',
    },
    Collapse: {
      headerBg: LightGraphTheme.white,
    },
    Input: {
      borderRadiusLG: 3,
      borderRadius: 3,
      borderRadiusXS: 3,
    },
    Select: {
      borderRadiusLG: 3,
      borderRadius: 3,
      borderRadiusXS: 3,
      optionSelectedColor: LightGraphTheme.white,
      optionSelectedBg: LightGraphTheme.primary,
    },
    Pagination: {
      fontFamily: LightGraphTheme.fontFamily,
    },
    Form: {
      labelColor: LightGraphTheme.subHeadingColor,
      colorBorder: 'none',
      verticalLabelPadding: 0,
      itemMarginBottom: 10,
    },
  },
};

export const darkTheme: ThemeConfig = {
  cssVar: true,
  algorithm: theme.darkAlgorithm,
  token: {
    colorBgBase: '#222222',
    colorPrimary: LightGraphTheme.primaryLight,
    fontFamily: LightGraphTheme.fontFamily,
    colorBorder: LightGraphTheme.borderGrayDark,
    colorTextDisabled: LightGraphTheme.textDisabled,
    colorBgContainerDisabled: LightGraphTheme.colorBgContainerDisabledDark,
  },
  components: {
    Tabs: {
      cardBg: '#F2F2F2',
      titleFontSize: 12,
    },
    Typography: {
      fontWeightStrong: 400,
    },
    Layout: {
      fontFamily: LightGraphTheme.fontFamily,
    },
    Menu: {
      itemSelectedBg: '#222222',
      itemSelectedColor: 'var(--ant-color-primary)',
    },
    Button: {
      borderRadius: 4,
      primaryColor: LightGraphTheme.white,
      defaultColor: 'var(--ant-color-text-base)',
      colorLink: LightGraphTheme.primaryLight,
      colorLinkHover: LightGraphTheme.primaryLight,
    },
    Table: {
      headerBg: 'var(--ant-color-bg-container)',
      padding: 18,
      borderColor: 'var(--ant-color-border)',
    },
    Collapse: {
      headerBg: LightGraphTheme.white,
    },
    Input: {
      borderRadiusLG: 3,
      borderRadius: 3,
      borderRadiusXS: 3,
    },
    Select: {
      borderRadiusLG: 3,
      borderRadius: 3,
      borderRadiusXS: 3,
      optionSelectedColor: LightGraphTheme.white,
      optionSelectedBg: LightGraphTheme.primary,
    },
    Pagination: {
      fontFamily: LightGraphTheme.fontFamily,
    },
    Form: {
      labelColor: 'var(--ant-color-text-base)',
      colorBorder: 'none',
      verticalLabelPadding: 0,
      itemMarginBottom: 10,
    },
  },
};
