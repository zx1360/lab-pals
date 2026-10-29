# 真由理手部与短袖修订 · 2026-10-29

使用内置 image_gen 编辑，要求真实透明背景。生成原图保存在 source/Mayuri-hands.png 与 source/Mayuri-sleep.png；通过 prepare-art.ps1 -Character Mayuri 对齐、分层和导出正式 PNG。运行时无需生成模型或 Node.js。

原作对照：https://steinsgate.jp/darling/common/img/cc2.png （官方《比翼恋理的爱人》角色立绘）。短泡泡袖之后是裸露前臂，初版桌宠增加的独立蓝色喇叭袖口不符合这张立绘。待机只接入下方手部区，保留基准头部和身体；睡姿整体导出。

## 待机手部（precise-object-edit）

输入：修订前 assets/characters/Mayuri.png，作为精确编辑目标和画风参考。

```text
Use case: precise-object-edit. Asset: transparent Mayuri keyboard-hands sprite source for Lab Pals. Supplied image is the exact edit target and approved character. Keep EVERYTHING above the dark trimmed short puff sleeves exactly unchanged: hat, bow, hair, face, eyes, head position, shoulders, torso, pink lace, waist bow, palette, linework, cel shading, scale, canvas composition. Correct only the lower forearms and hands: Mayuri wears pale blue puff SHORT sleeves ending in dark trim near the elbows, with bare forearms naturally continuing to the hands. REMOVE the incorrect separate blue bell-shaped detached cuffs flaring out to both sides of the hands at the very bottom. In their place show short anatomically continuous bare forearms and natural wrists. Keep both relaxed small human hands palms down in the SAME original keyboard-typing positions, left hand on viewer left and right hand on viewer right, fingertips pointed toward the bottom, all fingers clearly separated with plausible thumbs, preserve hand scale and fingertip baseline. Character still ends just below the fingertips. No long sleeves, no wrist bands, no detached cloth, no gloves, no added objects or keyboard. Preserve the waist bow behind the hands and the complete transparent margin. Single waist-up chibi character, no text, no effects, genuinely transparent alpha background.
```

## 睡姿（precise-object-edit）

输入 1：修订前 source/Mayuri-sleep.png，编辑目标。输入 2：修订前 assets/characters/Mayuri.png，角色与画风参考。旧图可从 Git 中恢复。

```text
Use case: precise-object-edit. Asset: replacement transparent PNG sleeping sprite for the Lab Pals Windows desktop companion. Image 1 is the edit target sleeping Mayuri; Image 2 is the approved awake character identity/style reference. Preserve Image 1's exact face, closed eyes, expression, head tilt, short charcoal hair, pale blue soft hat and blue bow, head position and scale, delicate dark outlines and restrained cel shading. REDRAW ONLY THE LOWER ARMS AND HANDS with clear correct human anatomy. The current crossed hands have ambiguous reversed wrist connections. Replace them with two relaxed small human hands resting naturally one over the other under her cheek, palms down, with clean distinct fingers and plausible thumbs. Each hand must connect continuously to its own wrist and forearm coming from the corresponding shoulder; avoid twisted wrists, extra fingers, fused hands, disconnected cuffs or duplicate skin patches. Simplify the overlap so the pose reads perfectly at 176 pixels wide. Costume has pale blue puff SHORT sleeves with dark trim, a short exposed forearm, and NO separate blue detached bell sleeves or floating cuffs around the wrists. Preserve pink lace neckline. Crop ends just below the folded hands. No furniture, no keyboard, no objects, no text, no symbols or ZZZ, no shadows outside character. Keep the same overall silhouette, composition and transparent surrounding canvas as Image 1. Genuine transparent alpha background.
```
