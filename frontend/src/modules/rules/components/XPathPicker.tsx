import { useState, useMemo } from 'react';
import { Modal, Input, Typography, Flex, Alert } from 'antd';
import { useTranslation } from 'react-i18next';

interface XPathPickerProps {
  open: boolean;
  onClose: () => void;
  onSelect: (xpath: string) => void;
}

interface XmlNode {
  localName: string;
  children: XmlNode[];
}

function parseXmlTree(xmlStr: string): { root: XmlNode | null; error: string | null } {
  try {
    const parser = new DOMParser();
    const doc = parser.parseFromString(xmlStr, 'text/xml');
    const parseError = doc.querySelector('parsererror');
    if (parseError) return { root: null, error: parseError.textContent ?? 'Parse error' };

    function walk(el: Element): XmlNode {
      const children: XmlNode[] = [];
      for (let i = 0; i < el.children.length; i++) {
        children.push(walk(el.children[i]));
      }
      return { localName: el.localName, children };
    }

    return { root: walk(doc.documentElement), error: null };
  } catch (e) {
    return { root: null, error: String(e) };
  }
}

const SKIP_NAMES = new Set(['Envelope', 'Header', 'Body']);

function TreeNode({
  node,
  onSelect,
  depth = 0,
}: {
  node: XmlNode;
  onSelect: (name: string) => void;
  depth?: number;
}) {
  const skip = SKIP_NAMES.has(node.localName);

  if (skip) {
    return (
      <>
        {node.children.map((child, i) => (
          <TreeNode key={i} node={child} onSelect={onSelect} depth={depth} />
        ))}
      </>
    );
  }

  return (
    <div style={{ paddingLeft: depth * 16 }}>
      <div
        style={{
          padding: '4px 8px',
          borderRadius: 6,
          cursor: 'pointer',
          fontSize: 13,
          fontFamily: 'monospace',
          transition: 'background 0.15s',
        }}
        onMouseEnter={(e) => {
          e.currentTarget.style.background = 'var(--color-primary-bg)';
        }}
        onMouseLeave={(e) => {
          e.currentTarget.style.background = 'transparent';
        }}
        onClick={() => onSelect(node.localName)}
      >
        &lt;{node.localName}&gt;
      </div>
      {node.children.map((child, i) => (
        <TreeNode key={i} node={child} onSelect={onSelect} depth={depth + 1} />
      ))}
    </div>
  );
}

export default function XPathPicker({ open, onClose, onSelect }: XPathPickerProps) {
  const { t } = useTranslation();
  const [xml, setXml] = useState('');

  const { root, error } = useMemo(() => {
    if (!xml.trim()) return { root: null, error: null };
    return parseXmlTree(xml);
  }, [xml]);

  const handleSelect = (localName: string) => {
    onSelect(`//*[local-name()='${localName}']`);
    onClose();
  };

  return (
    <Modal
      title={t('soap.pickXPath')}
      open={open}
      onCancel={onClose}
      footer={null}
      width={520}
      destroyOnClose
    >
      <Typography.Text type="secondary" style={{ fontSize: 12, display: 'block', marginBottom: 8 }}>
        {t('soap.pasteXml')}
      </Typography.Text>
      <Input.TextArea
        rows={6}
        value={xml}
        onChange={(e) => setXml(e.target.value)}
        placeholder={'<soapenv:Envelope xmlns:soapenv="...">\n  ...\n</soapenv:Envelope>'}
        style={{ fontFamily: 'monospace', fontSize: 12, marginBottom: 12 }}
      />
      {error && <Alert type="error" message={t('soap.parseError')} showIcon style={{ marginBottom: 12 }} />}
      {root && (
        <>
          <Flex style={{ marginBottom: 8 }}>
            <Typography.Text type="secondary" style={{ fontSize: 12 }}>
              {t('soap.selectElement')}
            </Typography.Text>
          </Flex>
          <div
            style={{
              maxHeight: 300,
              overflowY: 'auto',
              border: '1px solid var(--color-border)',
              borderRadius: 8,
              padding: 8,
            }}
          >
            <TreeNode node={root} onSelect={handleSelect} />
          </div>
        </>
      )}
    </Modal>
  );
}
