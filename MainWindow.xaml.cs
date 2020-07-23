using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using System.Runtime.InteropServices;
using System.Reflection;
using System.Diagnostics;

using MediaFoundation;
using MediaFoundation.ReadWrite;
using MediaFoundation.Transform;

namespace USBカメラMFWPF
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        class ComReleaser : IDisposable
        {
            private List<object> items = new List<object>();

            public void Add(object obj)
            {
                items.Add(obj);
            }

            public void Dispose()
            {
                foreach (var one in items)
                {
                    Marshal.ReleaseComObject(one);
                }

                items.Clear();
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            MF.EnumVideoDeviceSources(out IMFActivate[] devices);
            foreach (var device in devices)
            {
                device.GetString(MFAttributesClsid.MF_DEVSOURCE_ATTRIBUTE_FRIENDLY_NAME, out string name);
                device.GetString(MFAttributesClsid.MF_DEVSOURCE_ATTRIBUTE_SOURCE_TYPE_VIDCAP_SYMBOLIC_LINK, out string symLink);
                device.GetGUID(MFAttributesClsid.MF_DEVSOURCE_ATTRIBUTE_SOURCE_TYPE, out Guid type);
                devicesCombo.Items.Add(
                    new DeviceItem
                    {
                        Name = name,
                        Type = type,
                        SymLink = symLink,
                    }
                );
            }
        }

        class DeviceItem
        {
            public string Name { get; internal set; }
            public Guid Type { get; internal set; }
            public string SymLink { get; internal set; }

            public override string ToString() => Name;
        }

        private void devicesCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var item = (DeviceItem)devicesCombo.SelectedItem;
            if (item != null)
            {
                mediaCombo.Items.Clear();

                using (var releaser = new ComReleaser())
                {
                    MF.CreateVideoDeviceSource(item.SymLink, out IMFMediaSource source);
                    releaser.Add(source);
                    source.CreatePresentationDescriptor(out IMFPresentationDescriptor presDesc);
                    releaser.Add(presDesc);
                    presDesc.GetStreamDescriptorCount(out int descCount);
                    for (int descIndex = 0; descIndex < descCount; descIndex++)
                    {
                        presDesc.GetStreamDescriptorByIndex(descIndex, out bool selected, out IMFStreamDescriptor strmDesc);
                        releaser.Add(strmDesc);
                        strmDesc.GetMediaTypeHandler(out IMFMediaTypeHandler handler);
                        releaser.Add(handler);
                        handler.GetMediaTypeCount(out int typeCount);
                        for (int typeIndex = 0; typeIndex < typeCount; typeIndex++)
                        {
                            handler.GetMediaTypeByIndex(typeIndex, out IMFMediaType type);
                            releaser.Add(type);
                            type.GetSize(MFAttributesClsid.MF_MT_FRAME_SIZE, out uint width, out uint height);
                            type.GetGUID(MFAttributesClsid.MF_MT_SUBTYPE, out Guid subType);
                            type.GetUINT32(MFAttributesClsid.MF_MT_DEFAULT_STRIDE, out uint stride);
                            type.GetUINT32(MFAttributesClsid.MF_MT_SAMPLE_SIZE, out uint sampleSize);

                            mediaCombo.Items.Add(
                                new MediaItem
                                {
                                    Name = $"#{descIndex}.{typeIndex}: {width}x{height}, {GetSubTypeName(subType)}, {((int)stride)}, {sampleSize}",
                                    DescIndex = descIndex,
                                    TypeIndex = typeIndex,
                                    Width = (int)width,
                                    Height = (int)height,
                                    Stride = (int)stride,
                                    SampleSize = (int)sampleSize,
                                    DeviceItem = item,
                                    SubType = subType,
                                }
                            );
                        }
                    }
                }
            }
        }

        class MediaItem
        {
            public string Name { get; internal set; }
            public int Width { get; internal set; }
            public int Height { get; internal set; }
            public int Stride { get; internal set; }
            public int SampleSize { get; internal set; }
            public DeviceItem DeviceItem { get; internal set; }
            public int DescIndex { get; internal set; }
            public int TypeIndex { get; internal set; }
            public Guid SubType { get; internal set; }

            public override string ToString() => Name;
        }

        private string GetSubTypeName(Guid subType)
        {
            foreach (var field in typeof(MFMediaType).GetFields(BindingFlags.Static | BindingFlags.Public))
            {
                if (field.FieldType == typeof(Guid))
                {
                    if ((Guid)field.GetValue(null) == subType)
                    {
                        return field.Name;
                    }
                }
            }
            return null;
        }

        private void CaptureStillImages(MediaItem item)
        {
            using (var releaser = new ComReleaser())
            {
                MF.CreateVideoDeviceSource(item.DeviceItem.SymLink, out IMFMediaSource source);
                releaser.Add(source);
                source.CreatePresentationDescriptor(out IMFPresentationDescriptor presDesc);
                releaser.Add(presDesc);
                presDesc.GetStreamDescriptorByIndex(item.DescIndex, out bool selected, out IMFStreamDescriptor strmDesc);
                releaser.Add(strmDesc);
                strmDesc.GetMediaTypeHandler(out IMFMediaTypeHandler handler);
                releaser.Add(handler);
                handler.GetMediaTypeByIndex(item.TypeIndex, out IMFMediaType type);
                handler.SetCurrentMediaType(type);

                MF.CreateSourceReaderFromMediaSource(source, out IMFSourceReader reader);
                if (reader == null)
                {
                    return;
                }
                releaser.Add(reader);

                while (true)
                {
                    int frames = 0;
                    var hrRS = reader.ReadSample(
                        (int)MF_SOURCE_READER.AnyStream,
                        MF_SOURCE_READER_CONTROL_FLAG.None,
                        out int streamIndex,
                        out MF_SOURCE_READER_FLAG flags,
                        out long timeStamp,
                        out IMFSample sample
                    );

                    if (sample != null)
                    {
                        try
                        {
                            IMFSample rgbSample = sample;
                            rgbSample.GetBufferByIndex(0, out IMFMediaBuffer buff);
                            if (ConsumeBuffer(buff, item))
                            {
                                frames++;
                            }
                            else
                            {
                                return;
                            }
                        }
                        finally
                        {
                            Marshal.ReleaseComObject(sample);
                        }
                        break;
                    }
                }
                
            }
        }

        private bool ConsumeBuffer(IMFMediaBuffer buff, MediaItem item)
        {
            buff.Lock(out IntPtr ptr, out int maxLen, out int curLen);
            try
            {
                try
                {
                    byte[] temp = new byte[curLen];
                    Marshal.Copy(ptr, temp, 0, temp.Length);
                    int stride = item.Width * 3;
                    int tmpheight;

                    if (item.SubType.ToString() == "00000014-0000-0010-8000-00aa00389b71")//RGB
                    {
                        tmpheight = item.Height;
                    }else if(item.SubType.ToString() == "30323449-0000-0010-8000-00aa00389b71")//I420
                    {
                        //byte[] tout = new byte[stride * item.Height];
                        //Decoder.I420Decoder.DecompressI420(temp, item.Width, item.Height, tout);
                        //temp = tout;
                        //tmpheight = item.Height;
                        tmpheight = curLen / stride;
                    }
                    else
                    {
                        tmpheight = curLen / stride;
                    }
                    BitmapSource src = BitmapSource.Create(item.Width, tmpheight, 600, 600, PixelFormats.Bgr24, null, temp, stride);
                    image.Source = src;

                    return true;
                }
                catch (ObjectDisposedException)
                {
                    return false;
                }
            }
            finally
            {
                buff.Unlock();
            }
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            MediaItem item = mediaCombo.Items[mediaCombo.SelectedIndex] as MediaItem;

            CaptureStillImages(item);
        }
    }

}
