import React, { useState, useCallback } from 'react';
import { Button } from 'antd';
import { SnippetsOutlined, CheckOutlined } from '@ant-design/icons';
import { copyTextToClipboard } from '@/utils/jsonCopyUtils';
import LitegraphTooltip from '@/components/base/tooltip/Tooltip';

interface CopyButtonProps {
  text: string;
  tooltipTitle?: string;
}

const CopyButton = ({ text, tooltipTitle = 'Copy' }: CopyButtonProps) => {
  const [copied, setCopied] = useState(false);

  const handleCopy = useCallback(
    async (e: React.MouseEvent) => {
      e.stopPropagation();
      const ok = await copyTextToClipboard(text, '', false);
      if (ok) {
        setCopied(true);
        setTimeout(() => setCopied(false), 2000);
      }
    },
    [text]
  );

  return (
    <LitegraphTooltip title={copied ? 'Copied!' : tooltipTitle}>
      <Button
        type="text"
        size="small"
        icon={
          copied ? (
            <CheckOutlined style={{ color: '#52c41a', fontSize: 12 }} />
          ) : (
            <SnippetsOutlined style={{ fontSize: 12, color: '#bfbfbf' }} />
          )
        }
        onClick={handleCopy}
        style={{ padding: '0 4px', minWidth: 'auto', height: 'auto' }}
      />
    </LitegraphTooltip>
  );
};

export default CopyButton;
