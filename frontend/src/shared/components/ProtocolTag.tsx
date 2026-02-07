import { Tag } from 'antd';
import { ProtocolType, ProtocolTypeLabel } from '../types';

const colorMap: Record<ProtocolType, string> = {
  [ProtocolType.REST]: 'blue',
  [ProtocolType.SOAP]: 'orange',
  [ProtocolType.gRPC]: 'purple',
  [ProtocolType.GraphQL]: 'magenta',
};

interface ProtocolTagProps {
  protocol: ProtocolType;
}

export default function ProtocolTag({ protocol }: ProtocolTagProps) {
  return <Tag color={colorMap[protocol]}>{ProtocolTypeLabel[protocol]}</Tag>;
}
