#nullable enable
using AwesomeAssertions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DemonFighter.Editor.Tests.Presentation
{
    public sealed class DemonSkinShaderTests
    {
        private const string ShaderName = "DemonFighter/DemonSkin";

        [Test]
        public void DemonSkin_Exists_AndCompilesWithoutErrors()
        {
            Shader shader = Shader.Find(ShaderName);

            (shader != null).Should().BeTrue("the skin shader must be in the project");
            ShaderUtil.ShaderHasError(shader!).Should().BeFalse("the shader must compile; see the Console for the error");
            shader!.isSupported.Should().BeTrue();
        }

        [Test]
        public void DemonSkin_HasTheForwardShadowDepthAndNormalsPasses()
        {
            Shader shader = Shader.Find(ShaderName);
            ShaderData data = ShaderUtil.GetShaderData(shader);

            // The fallback shader contributes subshaders of its own; ours comes first.
            data.SubshaderCount.Should().BeGreaterThanOrEqualTo(1);
            ShaderData.Subshader subshader = data.GetSubshader(0);
            subshader.PassCount.Should().Be(4);
            subshader.GetPass(0).Name.Should().Be("ForwardLit");
            subshader.GetPass(1).Name.Should().Be("ShadowCaster");
            subshader.GetPass(2).Name.Should().Be("DepthOnly");
            subshader.GetPass(3).Name.Should().Be("DepthNormals");
        }

        [Test]
        public void DemonSkin_KeepsTheLitPropertiesAMaterialSwapReliesOn()
        {
            Shader shader = Shader.Find(ShaderName);

            shader.FindPropertyIndex("_BaseColor").Should().BeGreaterThanOrEqualTo(0);
            shader.FindPropertyIndex("_BaseMap").Should().BeGreaterThanOrEqualTo(0);
            shader.FindPropertyIndex("_BumpMap").Should().BeGreaterThanOrEqualTo(0);
            shader.FindPropertyIndex("_Smoothness").Should().BeGreaterThanOrEqualTo(0);
            shader.FindPropertyIndex("_Metallic").Should().BeGreaterThanOrEqualTo(0);
            shader.FindPropertyIndex("_EmissionColor").Should().BeGreaterThanOrEqualTo(0);
        }
    }
}
