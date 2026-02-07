import { Tag } from 'antd';

const colorMap: Record<string, string> = {
  GET: 'green',
  POST: 'blue',
  PUT: 'gold',
  PATCH: 'cyan',
  DELETE: 'red',
  HEAD: 'default',
  OPTIONS: 'default',
};

interface HttpMethodTagProps {
  method: string;
}

export default function HttpMethodTag({ method }: HttpMethodTagProps) {
  const upper = method.toUpperCase();
  return (
    <Tag color={colorMap[upper] || 'default'} style={{ fontWeight: 600 }}>
      {upper}
    </Tag>
  );
}
