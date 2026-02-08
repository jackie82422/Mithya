import { Input } from 'antd';

interface CodeEditorProps {
  value: string;
  onChange?: (value: string) => void;
  language?: string;
  height?: string | number;
  readOnly?: boolean;
}

function highlightJson(raw: string): string {
  const escaped = raw
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;');

  return escaped.replace(
    /("(?:\\.|[^"\\])*")\s*(:)?|\b(true|false|null)\b|(-?\d+(?:\.\d+)?(?:[eE][+-]?\d+)?)/g,
    (match, str, colon, bool, num) => {
      if (str) {
        if (colon) {
          return `<span style="color:var(--code-key)">${str}</span>:`;
        }
        return `<span style="color:var(--code-string)">${str}</span>`;
      }
      if (bool) return `<span style="color:var(--code-bool)">${match}</span>`;
      if (num) return `<span style="color:var(--code-number)">${match}</span>`;
      return match;
    },
  );
}

const monoFont =
  "'SF Mono', 'Fira Code', 'Cascadia Code', Menlo, Consolas, monospace";

export default function CodeEditor({
  value,
  onChange,
  height = 300,
  readOnly = false,
}: CodeEditorProps) {
  if (readOnly) {
    return (
      <pre
        style={{
          height: typeof height === 'number' ? height : undefined,
          fontFamily: monoFont,
          fontSize: 13,
          lineHeight: 1.6,
          background: 'var(--code-bg)',
          border: '1px solid var(--color-border)',
          borderRadius: 12,
          padding: '8px 12px',
          margin: 0,
          overflow: 'auto',
          color: 'var(--color-text)',
          whiteSpace: 'pre-wrap',
          wordBreak: 'break-word',
        }}
        dangerouslySetInnerHTML={{ __html: highlightJson(value || '') }}
      />
    );
  }

  return (
    <Input.TextArea
      value={value}
      onChange={(e) => onChange?.(e.target.value)}
      readOnly={readOnly}
      style={{
        height: typeof height === 'number' ? height : undefined,
        fontFamily: monoFont,
        fontSize: 13,
        lineHeight: 1.6,
        background: 'var(--color-surface)',
        border: '1px solid var(--color-border)',
        borderRadius: 12,
        resize: 'vertical',
        color: 'var(--color-text)',
      }}
      autoSize={false}
    />
  );
}
