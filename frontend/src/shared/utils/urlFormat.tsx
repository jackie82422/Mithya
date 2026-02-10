import React from 'react';

/**
 * Insert <wbr> after each `/` in a URL or path string,
 * so browsers only line-break at `/` boundaries when space is tight.
 */
export function breakableUrl(url: string): React.ReactNode[] {
  return url.split('/').flatMap((segment, i) => {
    if (i === 0) return [segment];
    return [
      '/',
      <wbr key={i} />,
      segment,
    ];
  });
}
