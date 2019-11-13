# Nano.Crypt Namespace

Nano.Crypt 位于工程 Nano.Extensive 中，用于提供一些加密的辅助工具。

## BlockCrypt 类
BlockCrypt 类用于以 16 字节为一组，ECB 模式的加密算法，例如 AES。并且对于尾部不足 16 字节的部分，使用 ITailTransform 来特殊处理以保证变换前后数据长度一致。

## ReadonlyTransformStream 类
ReadonlyTransformStream 可以在一个流上直接返回一个只读变换流。用于在流上直接读取加密/解密的数据。

ReadonlyTransformStream 类依赖 BlockCrypt 类。

---
[Home](../index)
