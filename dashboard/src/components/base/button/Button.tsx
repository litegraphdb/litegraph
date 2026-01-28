import { Button, ButtonProps } from 'antd';

interface LitegraphButtonProps extends ButtonProps {
  weight?: number;
}

const LitegraphButton = (props: LitegraphButtonProps) => {
  const { weight, icon, ...rest } = props;
  return <Button {...rest} icon={icon} style={{ fontWeight: weight }} />;
};

export default LitegraphButton;
