import { useState } from 'react';
import { Modal, Checkbox, Flex, Typography, Button, Input, ColorPicker } from 'antd';
import { PlusOutlined } from '@ant-design/icons';
import { useTranslation } from 'react-i18next';
import type { EndpointGroup } from '@/shared/types';
import { useCreateGroup, useAddEndpointsToGroup } from '../groupHooks';

interface GroupAssignModalProps {
  open: boolean;
  onClose: () => void;
  groups: EndpointGroup[];
  endpointIds: string[];
}

export default function GroupAssignModal({ open, onClose, groups, endpointIds }: GroupAssignModalProps) {
  const { t } = useTranslation();
  const [selectedGroupIds, setSelectedGroupIds] = useState<Set<string>>(new Set());
  const addEndpoints = useAddEndpointsToGroup();
  const createGroup = useCreateGroup();

  const [showCreate, setShowCreate] = useState(false);
  const [newName, setNewName] = useState('');
  const [newColor, setNewColor] = useState('#1677ff');

  const toggle = (id: string) => {
    setSelectedGroupIds((prev) => {
      const next = new Set(prev);
      if (next.has(id)) next.delete(id);
      else next.add(id);
      return next;
    });
  };

  const handleAssign = async () => {
    for (const groupId of selectedGroupIds) {
      await addEndpoints.mutateAsync({ groupId, endpointIds });
    }
    onClose();
    setSelectedGroupIds(new Set());
  };

  const handleCreate = () => {
    if (!newName.trim()) return;
    createGroup.mutate(
      { name: newName.trim(), color: newColor },
      {
        onSuccess: (created) => {
          setSelectedGroupIds((prev) => new Set([...prev, created.id]));
          setShowCreate(false);
          setNewName('');
        },
      },
    );
  };

  return (
    <Modal
      title={t('groups.addToGroup')}
      open={open}
      onCancel={() => { onClose(); setSelectedGroupIds(new Set()); }}
      onOk={handleAssign}
      confirmLoading={addEndpoints.isPending}
      okText={t('common.save')}
      cancelText={t('common.cancel')}
      okButtonProps={{ disabled: selectedGroupIds.size === 0 }}
      destroyOnClose
    >
      <Typography.Text type="secondary" style={{ fontSize: 12, display: 'block', marginBottom: 12 }}>
        {t('groups.assignEndpoints', { count: endpointIds.length })}
      </Typography.Text>

      <div style={{ maxHeight: 300, overflowY: 'auto' }}>
        {groups.map((g) => (
          <Flex
            key={g.id}
            align="center"
            gap={8}
            style={{
              padding: '8px 4px',
              borderBottom: '1px solid var(--color-border)',
              cursor: 'pointer',
            }}
            onClick={() => toggle(g.id)}
          >
            <Checkbox checked={selectedGroupIds.has(g.id)} />
            <span
              style={{
                display: 'inline-block',
                width: 10,
                height: 10,
                borderRadius: '50%',
                background: g.color || '#1677ff',
              }}
            />
            <Typography.Text>{g.name}</Typography.Text>
          </Flex>
        ))}
      </div>

      {showCreate ? (
        <Flex gap={8} align="center" style={{ marginTop: 12 }}>
          <ColorPicker
            size="small"
            value={newColor}
            onChange={(_, hex) => setNewColor(hex)}
          />
          <Input
            size="small"
            placeholder={t('groups.name')}
            value={newName}
            onChange={(e) => setNewName(e.target.value)}
            onPressEnter={handleCreate}
            style={{ flex: 1 }}
            autoFocus
          />
          <Button size="small" onClick={handleCreate} loading={createGroup.isPending}>
            {t('common.create')}
          </Button>
        </Flex>
      ) : (
        <Button
          type="dashed"
          icon={<PlusOutlined />}
          onClick={() => setShowCreate(true)}
          block
          style={{ marginTop: 12, borderRadius: 8 }}
        >
          {t('groups.create')}
        </Button>
      )}
    </Modal>
  );
}
