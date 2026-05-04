<template>
  <button 
    :class="[
      'base-btn', 
      `btn-${variant}`, 
      { 'btn-loading': loading, 'btn-icon': iconOnly }
    ]"
    :disabled="disabled || loading"
    @click="$emit('click', $event)"
  >
    <span v-if="loading" class="btn-spinner"></span>
    <span v-else class="btn-content">
      <slot name="icon-left"></slot>
      <slot></slot>
      <slot name="icon-right"></slot>
    </span>
  </button>
</template>

<script setup>
defineProps({
  variant: {
    type: String,
    default: 'primary',
    validator: (value) => ['primary', 'secondary', 'danger', 'ghost'].includes(value)
  },
  loading: {
    type: Boolean,
    default: false
  },
  disabled: {
    type: Boolean,
    default: false
  },
  iconOnly: {
    type: Boolean,
    default: false
  }
});

defineEmits(['click']);
</script>

<style scoped>
.base-btn {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: var(--space-2);
  padding: var(--space-2) var(--space-4);
  border-radius: var(--radius-md);
  font-size: 14px;
  font-weight: 500;
  cursor: pointer;
  transition: all 0.2s cubic-bezier(0.4, 0, 0.2, 1);
  border: 1px solid transparent;
  outline: none;
  white-space: nowrap;
  user-select: none;
}

.base-btn:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

/* Primary Variant */
.btn-primary {
  background: var(--primary);
  color: white;
  box-shadow: 0 4px 12px rgba(var(--primary-h), var(--primary-s), var(--primary-l), 0.2);
}

.btn-primary:hover:not(:disabled) {
  transform: translateY(-1px);
  box-shadow: 0 6px 16px rgba(var(--primary-h), var(--primary-s), var(--primary-l), 0.3);
  filter: brightness(1.1);
}

.btn-primary:active:not(:disabled) {
  transform: translateY(0);
}

/* Secondary Variant (Glass) */
.btn-secondary {
  background: var(--glass-bg);
  backdrop-filter: var(--glass-blur);
  -webkit-backdrop-filter: var(--glass-blur);
  border-color: var(--glass-border);
  color: var(--foreground);
}

.btn-secondary:hover:not(:disabled) {
  background: rgba(255, 255, 255, 0.4);
  border-color: rgba(255, 255, 255, 0.3);
}

/* Danger Variant */
.btn-danger {
  background: #ef4444;
  color: white;
}

.btn-danger:hover:not(:disabled) {
  background: #dc2626;
}

/* Ghost Variant */
.btn-ghost {
  background: transparent;
  color: var(--foreground);
}

.btn-ghost:hover:not(:disabled) {
  background: rgba(0, 0, 0, 0.05);
}

/* Loading Spinner */
.btn-spinner {
  width: 16px;
  height: 16px;
  border: 2px solid rgba(255, 255, 255, 0.3);
  border-top-color: white;
  border-radius: 50%;
  animation: spin 0.8s linear infinite;
}

.btn-secondary .btn-spinner {
  border-color: rgba(0, 0, 0, 0.1);
  border-top-color: var(--primary);
}

@keyframes spin {
  to { transform: rotate(360deg); }
}

.btn-content {
  display: flex;
  align-items: center;
  gap: var(--space-2);
}

.btn-icon {
  padding: var(--space-2);
  aspect-ratio: 1;
}
</style>
