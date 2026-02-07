import { ProtocolType, ProtocolTypeLabel } from '../types';

const styleMap: Record<ProtocolType, { bg: string; color: string }> = {
  [ProtocolType.REST]: { bg: 'var(--rest-bg)', color: 'var(--rest-color)' },
  [ProtocolType.SOAP]: { bg: 'var(--soap-bg)', color: 'var(--soap-color)' },
  [ProtocolType.gRPC]: { bg: 'var(--grpc-bg)', color: 'var(--grpc-color)' },
  [ProtocolType.GraphQL]: { bg: 'var(--graphql-bg)', color: 'var(--graphql-color)' },
};

interface ProtocolTagProps {
  protocol: ProtocolType;
}

export default function ProtocolTag({ protocol }: ProtocolTagProps) {
  const s = styleMap[protocol] ?? { bg: 'var(--inactive-bg)', color: 'var(--color-text-secondary)' };
  return (
    <span
      style={{
        display: 'inline-block',
        padding: '2px 10px',
        borderRadius: 100,
        fontSize: 12,
        fontWeight: 600,
        background: s.bg,
        color: s.color,
      }}
    >
      {ProtocolTypeLabel[protocol]}
    </span>
  );
}
