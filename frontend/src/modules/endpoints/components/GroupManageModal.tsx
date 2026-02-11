import { useState } from 'react';
import { Modal, Button, Input, Flex, Typography, Popconfirm, ColorPicker, Space } from 'antd';
import { PlusOutlined, EditOutlined, DeleteOutlined, CheckOutlined, CloseOutlined } from '@ant-design/icons';
import { useTranslation } from 'react-i18next';
import type { EndpointGroup } from '@/shared/types';
import { useCreateGroup, useUpdateGroup, useDeleteGroup } from '../groupHooks';

interface GroupManageModalProps {
  open: boolean;
  onClose: () => void;
  groups: EndpointGroup[];
}

const PRESET_COLORS = [
  '#1677ff', '#52c41a', '#faad14', '#f5222d', '#722ed1',
  '#eb2f96', '#13c2c2', '#fa8c16', '#2f54eb', '#a0d911',
];

export default function GroupManageModal({ open, onClose, groups }: GroupManageModalProps) {
  const { t } = useTranslation();
  const createGroup = useCreateGroup();
  const updateGroup = useUpdateGroup();
  const deleteGroup = useDeleteGroup();

  const [editingId, setEditingId] = useState<string | null>(null);
  const [editName, setEditName] = useState('');
  const [editColor, setEditColor] = useState('#1677ff');
  const [editDesc, setEditDesc] = useState('');

  const [creating, setCreating] = useState(false);
  const [newName, setNewName] = useState('');
  const [newColor, setNewColor] = useState('#1677ff');
  const [newDesc, setNewDesc] = useState('');

  const handleCreate = () => {
    if (!newName.trim()) return;
    createGroup.mutate(
      { name: newName.trim(), color: newColor, description: newDesc.trim() || undefined },
      {
        onSuccess: () => {
          setCreating(false);
          setNewName('');
          setNewDesc('');
        },
      },
    );
  };

  const handleUpdate = () => {
    if (!editingId || !editName.trim()) return;
    updateGroup.mutate(
      { id: editingId, data: { name: editName.trim(), color: editColor, description: editDesc.trim() || undefined } },
      { onSuccess: () => setEditingId(null) },
    );
  };

  const startEdit = (g: EndpointGroup) => {
    setEditingId(g.id);
    setEditName(g.name);
    setEditColor(g.color || '#1677ff');
    setEditDesc(g.description || '');
  };

  return (
    <Modal
      title={t('groups.manage')}
      open={open}
      onCancel={onClose}
      footer={null}
      width={500}
      destroyOnClose
    >
      <div style={{ maxHeight: 400, overflowY: 'auto' }}>
        {groups.map((g) => (
          <div
            key={g.id}
            style={{
              padding: '8px 0',
              borderBottom: '1px solid var(--color-border)',
            }}
          >
            {editingId === g.id ? (
              <div>
                <Flex gap={8} align="center" style={{ marginBottom: 4 }}>
                  <ColorPicker
                    size="small"
                    value={editColor}
                    presets={[{ label: '', colors: PRESET_COLORS }]}
                    onChange={(_, hex) => setEditColor(hex)}
                  />
                  <Input
                    size="small"
                    value={editName}
                    onChange={(e) => setEditName(e.target.value)}
                    style={{ flex: 1 }}
                    onPressEnter={handleUpdate}
                  />
                  <Button size="small" type="text" icon={<CheckOutlined />} onClick={handleUpdate} loading={updateGroup.isPending} />
                  <Button size="small" type="text" icon={<CloseOutlined />} onClick={() => setEditingId(null)} />
                </Flex>
                <Input
                  size="small"
                  placeholder={t('groups.description')}
                  value={editDesc}
                  onChange={(e) => setEditDesc(e.target.value)}
                  style={{ marginTop: 4 }}
                />
              </div>
            ) : (
              <Flex justify="space-between" align="center">
                <Flex align="center" gap={8}>
                  <span
                    style={{
                      display: 'inline-block',
                      width: 12,
                      height: 12,
                      borderRadius: '50%',
                      background: g.color || '#1677ff',
                      flexShrink: 0,
                    }}
                  />
                  <div>
                    <Typography.Text strong style={{ fontSize: 13 }}>{g.name}</Typography.Text>
                    <Typography.Text type="secondary" style={{ fontSize: 12, marginLeft: 8 }}>
                      {t('groups.endpointCount', { count: g.endpointCount ?? 0 })}
                    </Typography.Text>
                  </div>
                </Flex>
                <Space size={4}>
                  <Button size="small" type="text" icon={<EditOutlined />} onClick={() => startEdit(g)} />
                  <Popconfirm
                    title={t('groups.deleteConfirm')}
                    onConfirm={() => deleteGroup.mutate(g.id)}
                    okText={t('common.yes')}
                    cancelText={t('common.no')}
                  >
                    <Button size="small" type="text" danger icon={<DeleteOutlined />} />
                  </Popconfirm>
                </Space>
              </Flex>
            )}
          </div>
        ))}
      </div>

      {creating ? (
        <div style={{ marginTop: 12 }}>
          <Flex gap={8} align="center" style={{ marginBottom: 4 }}>
            <ColorPicker
              size="small"
              value={newColor}
              presets={[{ label: '', colors: PRESET_COLORS }]}
              onChange={(_, hex) => setNewColor(hex)}
            />
            <Input
              size="small"
              placeholder={t('groups.name')}
              value={newName}
              onChange={(e) => setNewName(e.target.value)}
              style={{ flex: 1 }}
              onPressEnter={handleCreate}
              autoFocus
            />
            <Button size="small" type="text" icon={<CheckOutlined />} onClick={handleCreate} loading={createGroup.isPending} />
            <Button size="small" type="text" icon={<CloseOutlined />} onClick={() => setCreating(false)} />
          </Flex>
          <Input
            size="small"
            placeholder={t('groups.description')}
            value={newDesc}
            onChange={(e) => setNewDesc(e.target.value)}
            style={{ marginTop: 4 }}
          />
        </div>
      ) : (
        <Button
          type="dashed"
          icon={<PlusOutlined />}
          onClick={() => setCreating(true)}
          block
          style={{ marginTop: 12, borderRadius: 8 }}
        >
          {t('groups.create')}
        </Button>
      )}
    </Modal>
  );
}
