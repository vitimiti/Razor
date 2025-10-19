// -----------------------------------------------------------------------
// <copyright file="DoubleNode`2.cs" company="Razor Project Authors">
// Copyright (c) Razor Project Authors. All rights reserved.
// Licensed under the MIT license.
// See LICENSE.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Razor.Vegas.Core.ListNodes;

/// <summary>
/// Represents a pair of associated values with linked-list nodes for both
/// primary and secondary aspects. Each <see cref="DoubleNode{TPrimary,TSecondary}"/>
/// contains two <see cref="DataNode{T}"/> instances that reference the
/// enclosing <see cref="DoubleNode{TPrimary,TSecondary}"/> via their
/// <see cref="DataNode{T}.Value"/> property.
/// </summary>
/// <typeparam name="TPrimary">Type of the primary value.</typeparam>
/// <typeparam name="TSecondary">Type of the secondary value.</typeparam>
public class DoubleNode<TPrimary, TSecondary>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DoubleNode{TPrimary,TSecondary}"/> class
    /// and sets up the internal data nodes.
    /// </summary>
    public DoubleNode() => Initialize();

    /// <summary>
    /// Initializes a new instance of the <see cref="DoubleNode{TPrimary,TSecondary}"/> class
    /// with the specified primary and secondary values and sets up the internal data nodes.
    /// </summary>
    /// <param name="primary">The initial primary value.</param>
    /// <param name="secondary">The initial secondary value.</param>
    public DoubleNode(TPrimary primary, TSecondary secondary)
    {
        Initialize();
        PrimaryValue = primary;
        SecondaryValue = secondary;
    }

    /// <summary>
    /// Gets or sets the primary payload value.
    /// </summary>
    public TPrimary? PrimaryValue { get; set; }

    /// <summary>
    /// Gets or sets the secondary payload value.
    /// </summary>
    public TSecondary? SecondaryValue { get; set; }

    /// <summary>
    /// Gets or sets the primary <see cref="DataNode{T}"/> which holds a reference to this <see cref="DoubleNode{TPrimary,TSecondary}"/>.
    /// </summary>
    public DataNode<DoubleNode<TPrimary, TSecondary>> Primary { get; set; } = new();

    /// <summary>
    /// Gets or sets the secondary <see cref="DataNode{T}"/> which holds a reference to this <see cref="DoubleNode{TPrimary,TSecondary}"/>.
    /// </summary>
    public DataNode<DoubleNode<TPrimary, TSecondary>> Secondary { get; set; } = new();

    /// <summary>
    /// Unlinks both the primary and secondary internal nodes from their lists.
    /// </summary>
    public void Unlink()
    {
        Primary.Unlink();
        Secondary.Unlink();
    }

    private void Initialize()
    {
        Primary.Value = this;
        Secondary.Value = this;
    }
}
