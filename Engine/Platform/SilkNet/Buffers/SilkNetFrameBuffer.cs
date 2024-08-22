using System.Diagnostics;
using Engine.Renderer.Buffers;
using Engine.Renderer.Buffers.FrameBuffer;
using Silk.NET.OpenGL;

namespace Engine.Platform.SilkNet.Buffers;

public class SilkNetFrameBuffer : FrameBuffer
{
    private const uint MaxFramebufferSize = 8192;
    
    private uint _rendererId = 0;
    private readonly FrameBufferSpecification _specification;
    private List<FramebufferTextureSpecification> _colorAttachmentSpecifications = [];
    FramebufferTextureSpecification _depthAttachmentSpecification = new FramebufferTextureSpecification(FramebufferTextureFormat.None);
    private List<uint> _colorAttachments = [];
    uint _depthAttachment = 0;

    public SilkNetFrameBuffer(FrameBufferSpecification spec)
    {
        _specification = spec;
        
        foreach (var specificationAttachment in _specification.Attachments.Attachments)
        {
            if (!Utils.IsDepthFormat(specificationAttachment.TextureFormat))
                _colorAttachmentSpecifications.Add(specificationAttachment);
            else
                _depthAttachmentSpecification = specificationAttachment;
        }

        
        Invalidate();
    }

    ~SilkNetFrameBuffer()
    {
        SilkNetContext.GL.DeleteFramebuffers(1, _rendererId);
        SilkNetContext.GL.DeleteTextures(_colorAttachments.ToArray().AsSpan());
        SilkNetContext.GL.DeleteRenderbuffer(_depthAttachment);
    }

    public override uint GetColorAttachmentRendererId(uint index = 0) => _colorAttachments.Count == index - 1 ? _colorAttachments[(int)index] : 0;
    public override FrameBufferSpecification GetSpecification() => _specification;

    public override void Resize(uint width, uint height)
    {
        if (width == 0 || height == 0 || width > MaxFramebufferSize || height > MaxFramebufferSize)
        {
            Debug.WriteLine("Attempted to resize framebuffer to {0}, {1}", width, height);
            return;
        }
        
        _specification.Width = width;
        _specification.Height = height;

        Invalidate();
    }

    public override void Bind()
    {
        SilkNetContext.GL.BindFramebuffer(FramebufferTarget.Framebuffer, _rendererId);
        //SilkNetContext.GL.Viewport(0, 0, _specification.Width, _specification.Height);
    }

    public override void Unbind()
    {
        SilkNetContext.GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
    }

    private void Invalidate()
    {
        unsafe
        {
            if (_rendererId != 0)
            {
                SilkNetContext.GL.DeleteFramebuffer(_rendererId);

                SilkNetContext.GL.DeleteTextures(_colorAttachments.ToArray().AsSpan());

                SilkNetContext.GL.DeleteRenderbuffer(_depthAttachment);
            }

            _rendererId = SilkNetContext.GL.GenFramebuffer();
            SilkNetContext.GL.BindFramebuffer(FramebufferTarget.Framebuffer, _rendererId);
            SilkNetContext.GL.DrawBuffer(DrawBufferMode.ColorAttachment0);

            bool multisample = _specification.Samples > 1;

        // Color Attachments
        if (_colorAttachmentSpecifications.Count > 0)
        {
            var colorAttachments = new uint[_colorAttachmentSpecifications.Count];
            Utils.CreateTextures(SilkNetContext.GL, multisample, colorAttachments.ToArray(), (uint)_colorAttachmentSpecifications.Count);
            _colorAttachments = colorAttachments.ToList();
            
            for (int i = 0; i < _colorAttachments.Count; i++)
            {
                Utils.BindTexture(SilkNetContext.GL, multisample, _colorAttachments[i]);

                switch (_colorAttachmentSpecifications[i].TextureFormat)
                {
                    case FramebufferTextureFormat.RGBA8:
                        Utils.AttachColorTexture(SilkNetContext.GL, _colorAttachments[i], _specification.Samples, GLEnum.Rgba8, _specification.Width, _specification.Height, i);
                        break;
                }
            }
        }

        // Depth Attachment
        if (_depthAttachmentSpecification.TextureFormat != FramebufferTextureFormat.None)
        {
            // var depthAttachment = new uint[1];
            // Utils.CreateTextures(SilkNetContext.GL, multisample, depthAttachment, 1);
            // _depthAttachment = depthAttachment[0];
            // Utils.BindTexture(SilkNetContext.GL, multisample, _depthAttachment);
            //
            // switch (_depthAttachmentSpecification.TextureFormat)
            // {
            //     case FramebufferTextureFormat.DEPTH24STENCIL8:
            //         Utils.AttachDepthTexture(SilkNetContext.GL, _depthAttachment, _specification.Samples, GLEnum.Depth24Stencil8, GLEnum.DepthStencilAttachment, _specification.Width, _specification.Height);
            //         break;
            // }
            
            // Generate handle
            _depthAttachment = SilkNetContext.GL.GenRenderbuffer();
            
            SilkNetContext.GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, _depthAttachment);
            SilkNetContext.GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, InternalFormat.Depth24Stencil8,
                _specification.Width, _specification.Height);
            SilkNetContext.GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer,
                FramebufferAttachment.DepthStencilAttachment, RenderbufferTarget.Renderbuffer, _depthAttachment);
            
            // Check framebuffer completeness
            var status = SilkNetContext.GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
            if (status != GLEnum.FramebufferComplete)
            {
                throw new Exception($"Framebuffer is not complete: {status}");
            }
        }

        // Handle draw buffers
        if (_colorAttachments.Count > 1)
        {
            if (_colorAttachments.Count > 4)
            {
                throw new Exception("Too many color attachments!");
            }

            GLEnum[] buffers = { GLEnum.ColorAttachment0, GLEnum.ColorAttachment1, GLEnum.ColorAttachment2, GLEnum.ColorAttachment3 };
            SilkNetContext.GL.DrawBuffers((uint)_colorAttachments.Count, buffers);
        }
        else if (_colorAttachments.Count == 0)
        {
            // Only depth-pass
            SilkNetContext.GL.DrawBuffer(GLEnum.None);
        }

        
            // Unbind framebuffer
            SilkNetContext.GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        }
    }
}