using System;
using System.Collections.Generic;
using System.Text;
using Windows.Storage;
using Windows.Storage.Streams;
using System.IO;
using LunaVK.Core.DataObjects;
using LunaVK.Core.Network;
using LunaVK.Core.Utils;
using LunaVK.Core.Framework;
using Windows.UI.Xaml;
using LunaVK.Core.ViewModels;

namespace LunaVK.Core.Library
{
    public class OutboundVoiceMessageAttachment : ViewModelBase, IOutboundAttachment
    {
        private StorageFile _file;
        private List<int> _waveform;
        private string _filePath;
        private int _duration;
        private DocPreview.DocPreviewVoiceMessage _savedDoc;

        private double _uploadProgress;

        private OutboundAttachmentUploadState _uploadState;

        private bool _retryFlag = true;

        public OutboundAttachmentUploadState UploadState
        {
            get { return this._uploadState; }
            set
            {
                this._uploadState = value;
                base.NotifyPropertyChanged(nameof(this.UploadState));
                base.NotifyPropertyChanged(nameof(this.IsUploadingVisibility));
                base.NotifyPropertyChanged(nameof(this.IsFailedUploadVisibility));
                base.NotifyPropertyChanged(nameof(this.UploadProgress));
            }
        }

        public double UploadProgress
        {
            get { return this._uploadProgress; }
            set
            {
                this._uploadProgress = value;
                base.NotifyPropertyChanged(nameof(this.UploadProgress));
            }
        }

        public /*override*/ Visibility IsFailedUploadVisibility
        {
            get
            {
                if (this._uploadState != OutboundAttachmentUploadState.Failed)
                    return Visibility.Collapsed;
                return Visibility.Visible;
            }
        }

        public Visibility IsUploadingVisibility
        {
            get
            {
                if (this.UploadState != OutboundAttachmentUploadState.Uploading)
                    return Visibility.Collapsed;
                return Visibility.Visible;
            }
        }

        public bool IsUploadAttachment
        {
            get { return true; }
        }

        public OutboundVoiceMessageAttachment(StorageFile file, int duration, List<int> waveform)
        {
            this._file = file;
            this._filePath = file.Path;
            this._duration = duration;
            this._waveform = waveform;
            this.UploadState = OutboundAttachmentUploadState.NotStarted;
        }

        public OutboundVoiceMessageAttachment()
        {
        }

        public void Upload(Action completionCallback, Action<double> progressCallback = null)
        {
            // Guard null completionCallback to avoid null invocation
            Action safeCompletion = completionCallback ?? (() => { });

            this.UploadState = OutboundAttachmentUploadState.Uploading;
            this.UploadProgress = 0.0;

            DocumentsService.Instance.ReadFully(this._file, (bytes) =>
            {
                DocumentsService.Instance.UploadVoiceMessageDocument(bytes, this._waveform, (doc) =>
                {
                    if (doc != null)
                    {
                        this.UploadState = OutboundAttachmentUploadState.Completed;
                        this._savedDoc = doc.audio_message;
                        this.UploadProgress = 100.0;
                    }
                    else
                    {
                        this.UploadState = OutboundAttachmentUploadState.Failed;
                        if (this._retryFlag)
                        {
                            // try once more
                            this._retryFlag = false;
                            try
                            {
                                this.Upload(completionCallback, progressCallback);
                                return;
                            }
                            catch { }
                        }
                    }

                    safeCompletion();
                }, (progress) =>
                {
                    try
                    {
                        this.UploadProgress = progress;
                        progressCallback?.Invoke(progress);
                    }
                    catch { }
                });
            });
        }

        public override string ToString()
        {
            uint owner = Settings.UserId;
            return string.Format("{0}{1}_{2}", "audio_message", owner, this._savedDoc == null ? 0 : this._savedDoc.id);
        }

        public VKAttachment GetAttachment()
        {
            VKAttachment attachment = new VKAttachment();
            attachment.type = Enums.VKAttachmentType.Doc;
            VKDocument doc = new VKDocument() { type = Enums.VKDocumentType.AUDIO };
            doc.preview = new DocPreview();
            doc.preview.audio_msg = this._savedDoc ?? new DocPreview.DocPreviewVoiceMessage();
            attachment.doc = doc;
            return attachment;
        }

        public void Write(BinaryWriter writer)
        {
            writer.Write(1);
            writer.WriteString(this._filePath);
            writer.Write(this._duration);
            BinarySerializerExtensions.WriteList(writer, this._waveform);
            writer.Write((byte)this.UploadState);
            writer.Write<DocPreview.DocPreviewVoiceMessage>(this._savedDoc);
        }

        public void Read(BinaryReader reader)
        {
            reader.ReadInt32();
            this._filePath = reader.ReadString();
            this._duration = reader.ReadInt32();
            this._waveform = BinarySerializerExtensions.ReadListInt(reader);
            this.UploadState = (OutboundAttachmentUploadState)reader.ReadByte();
            if (this.UploadState == OutboundAttachmentUploadState.Uploading)
                this.UploadState = OutboundAttachmentUploadState.Failed;
            this._savedDoc = reader.ReadGeneric<DocPreview.DocPreviewVoiceMessage>();
        }
    }
}
