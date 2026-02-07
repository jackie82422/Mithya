import { Input } from 'antd';

interface CodeEditorProps {
  value: string;
  onChange?: (value: string) => void;
  language?: string;
  height?: string | number;
  readOnly?: boolean;
}

export default function CodeEditor({
  value,
  onChange,
  height = 300,
  readOnly = false,
}: CodeEditorProps) {
  return (
    <Input.TextArea
      value={value}
      onChange={(e) => onChange?.(e.target.value)}
      readOnly={readOnly}
      style={{
        height: typeof height === 'number' ? height : undefined,
        fontFamily: "'SF Mono', 'Fira Code', 'Cascadia Code', Menlo, Consolas, monospace",
        fontSize: 13,
        lineHeight: 1.6,
        background: readOnly ? '#f5f5f5' : '#fff',
        resize: 'vertical',
      }}
      autoSize={false}
    />
  );
}
