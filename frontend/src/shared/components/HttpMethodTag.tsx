const styleMap: Record<string, { bg: string; color: string }> = {
  GET: { bg: 'var(--get-bg)', color: 'var(--get-color)' },
  POST: { bg: 'var(--post-bg)', color: 'var(--post-color)' },
  PUT: { bg: 'var(--put-bg)', color: 'var(--put-color)' },
  PATCH: { bg: 'var(--patch-bg)', color: 'var(--patch-color)' },
  DELETE: { bg: 'var(--delete-bg)', color: 'var(--delete-color)' },
};

const defaultStyle = { bg: 'var(--inactive-bg)', color: 'var(--color-text-secondary)' };

interface HttpMethodTagProps {
  method: string;
}

export default function HttpMethodTag({ method }: HttpMethodTagProps) {
  const upper = method.toUpperCase();
  const s = styleMap[upper] ?? defaultStyle;
  return (
    <span
      style={{
        display: 'inline-block',
        padding: '2px 10px',
        borderRadius: 100,
        fontSize: 12,
        fontWeight: 600,
        textTransform: 'uppercase',
        background: s.bg,
        color: s.color,
        letterSpacing: '0.3px',
      }}
    >
      {upper}
    </span>
  );
}
