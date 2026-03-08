using PaintDotNet;
using PaintDotNet.Effects;
using System;
using System.Reflection;

namespace RedKnack.HalftonePlugin
{
    public sealed class PluginSupportInfo : IPluginSupportInfo
    {
        private static readonly Assembly _asm = typeof(PluginSupportInfo).Assembly;

        public string Author      => "RedKnack Interactive";
        public string Copyright   => "© RedKnack Interactive - redknack.com";
        public string DisplayName => "Halftone Comic/Print";
        public Version Version    => _asm.GetName().Version ?? new Version(1, 3, 0);
        public Uri WebsiteUri     => new Uri("https://redknack.com");
    }
}
