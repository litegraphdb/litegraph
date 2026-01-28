import { Avatar, AvatarProps } from 'antd';
import React from 'react';

export type LitegraphAvatarProps = AvatarProps;

const LitegraphAvatar = (props: LitegraphAvatarProps) => {
  return <Avatar {...props} />;
};

export default LitegraphAvatar;
