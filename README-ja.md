# Viewseprite

[English version](./README.md)

AsepriteのスプライトをUnityにリアルタイムで表示できるユーティリティです。

編集中のスプライトをシェーディングやライティング、ポストプロセッシングなどを適用した状態で確認できます。

![image](https://user-images.githubusercontent.com/16096562/139103047-8df604ad-e0f5-40f3-9d30-43693d48c94d.png)

## 要件
 - Unity 2020.3 で動作を確認しています
 - **Aseprite v1.2.30以降**

## サンプルを動かす
1. `scripts/Launch Viewseprite.lua` をAsepriteのスクリプトフォルダ (File > Scripts > Open Script Folder から確認できます) にコピーする 
2. ViewsepriteのUnityプロジェクトを開きPlayする
3. Asepriteでなんらかのファイルを開き、File > Scripts メニューから `Launch Viewseprite` を選択する

## 自分のプロジェクトで使う
UPM の git dependencies を使います。
1. Package Managerで `+` > `Add package from git URL...` を押す
2. `https://github.com/ruccho/Viewseprite.git?path=/Viewseprite/Packages/io.github.ruccho.viewseprite` を入力する

## 使い方
Viewseprite は以下のような Grabber コンポーネントから使用します。表示方法に応じて異なるコンポーネントが提供されています：
 - `GrabberForSpriteRenderer`: SpriteRendererを使ってスプライトを表示します。
 - `GrabberForRenderer`: MeshRendererをはじめとする各Rendererで表示します。
 - `GrabberForImage`: uGUIのImageとして表示します。

カスタマイズするためには、`GrabberBase` を継承して `SetTexture(Texture2D texture)` を実装します。

### Layerの指定
それぞれのGrabberコンポーネントには `Visible Layer` プロパティがあり、表示するレイヤー名を指定することができます。一致するレイヤーがない場合はスプライト全体が表示されます。

レイヤーの指定は毎回の接続のスタート時にのみ適用されるため、レイヤーを変更するには、ゲームを再スタートするかAseprite側でViewsepriteを再度開いてください。

## 参照
- [lampysprites/aseprite-interprocessing-demo](https://github.com/lampysprites/aseprite-interprocessing-demo)