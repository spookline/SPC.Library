using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

namespace Spookline.SPC.Ext {
    public abstract class AttachmentSpookBehaviour<TSelf, TAttachment> : SpookBehaviour<TSelf>
        where TSelf : AttachmentSpookBehaviour<TSelf, TAttachment>
        where TAttachment : IAttachment {

        [OdinSerialize, LabelText("Attachments")]
        private List<TAttachment> _inlineAttachments = new();

        private readonly List<TAttachment> _attachments = new();
        private readonly List<Component> _foundAttachments = new();
        private readonly Dictionary<Type, TAttachment> _attachmentsByType = new();
        private bool _attachmentsDirty = true;
        private uint _revision;

        [PropertySpace, ShowInInspector, HideInEditorMode, LabelText("Runtime Attachments")]
        public IReadOnlyList<TAttachment> Attachments {
            get {
                RefreshAttachmentsIfDirty();
                return _attachments;
            }
        }

        public uint Revision => _revision;

        public void RefreshAttachments() {
            _attachments.Clear();
            _attachmentsByType.Clear();

            GetComponents(_foundAttachments);
            _attachments.Capacity = _foundAttachments.Count + _inlineAttachments.Count;
            _attachments.AddRange(_inlineAttachments);
            foreach (var attachment in _foundAttachments) {
                if (!(attachment is TAttachment typedAttachment)) continue;
                _attachments.Add(typedAttachment);
            }

            foreach (var attachment in _attachments) {
                var type = attachment.GetType();
                if (_attachmentsByType.TryAdd(type, attachment)) continue;
                Debug.LogWarning(
                    $"Multiple attachments of type {type} found on {name}. Only the first one will be accessible via GetAttachment<{type}>.");
            }

            _revision++;
        }

        public AttachmentAccessor<T> GetAccessor<T>() where T : TAttachment {
            return new AttachmentAccessor<T, TAttachment, TSelf>((TSelf)this);
        }

        public AttachmentAccessor<T> GetConcreteAccessor<T>() where T : TAttachment {
            return new AttachmentAccessor<T, TAttachment, TSelf>((TSelf)this, true);
        }

        public T GetAttachment<T>() where T : TAttachment {
            RefreshAttachmentsIfDirty();
            return _attachmentsByType.TryGetValue(typeof(T), out var attachment) ? (T)attachment : default;
        }

        public void PutAttachment<T>(T attachment) where T : TAttachment {
            _inlineAttachments.RemoveAll(x => x is T);
            _inlineAttachments.Add(attachment);
            MarkAttachmentsDirty();
        }

        public void RemoveAttachment<T>() where T : TAttachment {
            _inlineAttachments.RemoveAll(x => x is T);
            MarkAttachmentsDirty();
        }

        public T GetAttachmentOf<T>() where T : TAttachment {
            RefreshAttachmentsIfDirty();
            var found = _attachments.FirstOrDefault(x => x is T);
            return found != null ? (T)found : default;
        }

        public bool TryGetAttachment<T>(out T attachment) where T : TAttachment {
            RefreshAttachmentsIfDirty();
            if (_attachmentsByType.TryGetValue(typeof(T), out var found)) {
                attachment = (T)found;
                return true;
            }

            attachment = default;
            return false;
        }

        public bool TryGetAttachmentOf<T>(out T attachment) where T : TAttachment {
            RefreshAttachmentsIfDirty();
            var found = _attachments.FirstOrDefault(x => x is T);
            if (found != null) {
                attachment = (T)found;
                return true;
            }

            attachment = default;
            return false;
        }

        public bool HasAttachment<T>() where T : TAttachment {
            RefreshAttachmentsIfDirty();
            return _attachmentsByType.ContainsKey(typeof(T));
        }

        public bool HasAttachmentOf<T>() where T : TAttachment {
            RefreshAttachmentsIfDirty();
            return _attachments.Any(x => x is T);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void MarkAttachmentsDirty() {
            _attachmentsDirty = true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RefreshAttachmentsIfDirty() {
            if (!_attachmentsDirty) return;
            RefreshAttachments();
            _attachmentsDirty = false;
        }

        protected virtual void Awake() {
            RefreshAttachmentsIfDirty();
        }

        protected override void Start() {
            base.Start();
            RefreshAttachmentsIfDirty();
        }

        protected override void OnEnable() {
            base.OnEnable();
            RefreshAttachmentsIfDirty();
        }

        protected override void OnDisable() {
            base.OnDisable();
            RefreshAttachmentsIfDirty();
        }

    }

    public interface IAttachment { }

    public abstract class AttachmentAccessor<TValue> {

        public abstract TValue Value { get; }
        public abstract TValue ValueOrThrow { get; }
        public abstract bool HasValue { get; }

        public bool TryGet(out TValue value) {
            if (HasValue) {
                value = Value;
                return true;
            }

            value = default;
            return false;
        }

        public static implicit operator TValue(AttachmentAccessor<TValue> accessor) => accessor.Value;

    }

    public class AttachmentAccessor<TValue, TAttachment, TBehaviour> : AttachmentAccessor<TValue>, IAttachment
        where TAttachment : IAttachment
        where TValue : TAttachment
        where TBehaviour : AttachmentSpookBehaviour<TBehaviour, TAttachment> {

        private readonly TBehaviour _behaviour;
        private readonly bool _concrete;
        private uint _revision;
        private TValue _value;
        private bool _hasValue;

        public AttachmentAccessor(TBehaviour behaviour, bool concrete = false) {
            _behaviour = behaviour;
            _concrete = concrete;
            RefreshIfNeeded();
        }

        public override TValue Value {
            get {
                if (!_behaviour) return default;
                RefreshIfNeeded();
                return _value;
            }
        }

        public override TValue ValueOrThrow {
            get {
                if (!_behaviour) throw new InvalidOperationException("Behaviour is null.");
                RefreshIfNeeded();
                if (!_hasValue)
                    throw new InvalidOperationException(
                        $"Attachment of type {typeof(TValue)} not found on {typeof(TBehaviour)}.");
                return _value;
            }
        }

        public override bool HasValue {
            get {
                if (!_behaviour) return false;
                RefreshIfNeeded();
                return _hasValue;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void RefreshIfNeeded() {
            if (_behaviour.Revision == _revision) return;
            _revision = _behaviour.Revision;
            _hasValue = _concrete ? _behaviour.TryGetAttachment(out _value) : _behaviour.TryGetAttachmentOf(out _value);
        }

    }
}