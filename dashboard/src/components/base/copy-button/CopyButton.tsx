import React, { useState, useCallback, useEffect, useRef } from 'react';
import { Button, ButtonProps } from 'antd';
import { SnippetsOutlined, CheckOutlined } from '@ant-design/icons';
import { copyTextToClipboard } from '@/utils/jsonCopyUtils';
import LitegraphTooltip from '@/components/base/tooltip/Tooltip';

interface CopyButtonProps {
  text?: string;
  getText?: () => string;
  tooltipTitle?: string;
  label?: React.ReactNode;
  type?: ButtonProps['type'];
  size?: ButtonProps['size'];
  style?: React.CSSProperties;
  className?: string;
  disabled?: boolean;
  stopPropagation?: boolean;
}

const CopyButton = ({
  text,
  getText,
  tooltipTitle = 'Copy',
  label,
  type = 'text',
  size = 'small',
  style,
  className,
  disabled,
  stopPropagation = true,
}: CopyButtonProps) => {
  const [copied, setCopied] = useState(false);
  const resetTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  const handleCopy = useCallback(
    async (e: React.MouseEvent) => {
      if (stopPropagation) {
        e.stopPropagation();
      }

      const value = getText ? getText() : text || '';
      const ok = await copyTextToClipboard(value, '', false);
      if (ok) {
        if (resetTimerRef.current) {
          clearTimeout(resetTimerRef.current);
        }

        setCopied(true);
        resetTimerRef.current = setTimeout(() => setCopied(false), 2000);
      }
    },
    [getText, stopPropagation, text]
  );

  useEffect(() => {
    return () => {
      if (resetTimerRef.current) {
        clearTimeout(resetTimerRef.current);
      }
    };
  }, []);

  const icon = copied ? (
    <span data-testid="copy-check-icon" style={{ color: '#52c41a', fontSize: 12 }}>
      <CheckOutlined />
    </span>
  ) : (
    <span style={{ color: '#bfbfbf', fontSize: 12 }}>
      <SnippetsOutlined />
    </span>
  );

  return (
    <LitegraphTooltip title={copied ? 'Copied!' : tooltipTitle}>
      <Button
        type={type}
        size={size}
        aria-label={tooltipTitle}
        icon={icon}
        onClick={handleCopy}
        style={
          label
            ? style
            : { padding: '0 4px', minWidth: 'auto', height: 'auto', ...style }
        }
        className={className}
        disabled={disabled}
      >
        {label}
      </Button>
    </LitegraphTooltip>
  );
};

export default CopyButton;
