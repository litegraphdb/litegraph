'use client';

import ace from 'ace-builds/src-noconflict/ace';
import 'ace-builds/src-noconflict/ext-language_tools';
import 'ace-builds/src-noconflict/mode-json';
import 'ace-builds/src-noconflict/theme-textmate';
import 'ace-builds/src-noconflict/theme-tomorrow_night';
import { JsonEditor } from 'jsoneditor-react';
import { useAppContext } from '@/hooks/appHooks';
import { ThemeEnum } from '@/types/types';

/**
 * jsoneditor wrapper that keeps the embedded Ace code view in step with the
 * application theme. The jsoneditor chrome (menu, tree, statusbar) is styled
 * with CSS variables in globals.scss, but the Ace editor ships its own themed
 * stylesheet, so a fixed light theme (textmate) renders a bright code panel
 * inside an otherwise dark UI. Selecting a dark Ace theme in dark mode keeps
 * the two consistent. Keying the editor on the theme forces jsoneditor to
 * re-initialize when the mode toggles, since it reads the theme only on mount.
 */
const JsonEditorWithAce = (props: any) => {
  const { theme } = useAppContext();
  const isDark = theme === ThemeEnum.DARK;
  const aceTheme = isDark ? 'ace/theme/tomorrow_night' : 'ace/theme/textmate';

  return <JsonEditor key={isDark ? 'dark' : 'light'} ace={ace} theme={aceTheme} {...props} />;
};

export default JsonEditorWithAce;
