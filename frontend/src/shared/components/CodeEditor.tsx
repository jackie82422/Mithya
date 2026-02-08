import { useMemo } from 'react';
import CodeMirror from '@uiw/react-codemirror';
import { json } from '@codemirror/lang-json';
import { useTheme } from '@/shared/contexts/ThemeContext';

interface CodeEditorProps {
  value: string;
  onChange?: (value: string) => void;
  language?: 'json' | 'handlebars';
  height?: string | number;
  readOnly?: boolean;
}

export default function CodeEditor({
  value,
  onChange,
  language = 'json',
  height = 300,
  readOnly = false,
}: CodeEditorProps) {
  const { mode } = useTheme();
  const extensions = useMemo(() => {
    if (language === 'handlebars') return [];
    return [json()];
  }, [language]);
  const h = typeof height === 'number' ? `${height}px` : height;

  return (
    <CodeMirror
      value={value}
      onChange={readOnly ? undefined : onChange}
      extensions={extensions}
      theme={mode === 'dark' ? 'dark' : 'light'}
      readOnly={readOnly}
      editable={!readOnly}
      height={h}
      style={{ borderRadius: 12, overflow: 'hidden', border: '1px solid var(--color-border)' }}
      basicSetup={{
        lineNumbers: true,
        foldGutter: true,
        bracketMatching: true,
        closeBrackets: true,
        highlightActiveLine: !readOnly,
      }}
    />
  );
}
