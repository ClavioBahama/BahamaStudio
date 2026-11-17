# Bahama Studio

Gerenciador independente e visual para o **ExplorerBlurMica 2.1.0** — a engine que aplica
Blur, Acrílico e Mica nas janelas do Explorador de Arquivos do Windows.

**Criado por Clávio Bahama.**

O Bahama Studio não reimplementa o hook do shell. Ele empacota a engine oficial, instala com
backup, testa o resultado e desfaz sozinho se o Explorador falhar.

![Fluxo](assets/BahamaStudio-256.png)

---

## Por que esta versão existe

A versão 2.0.0 do ExplorerBlurMica travava o `explorer.exe` no Windows 11 24H2
(build 26100) com falha em `ucrtbase.dll` (`0xc0000005`, evento 1000).

A engine **2.1.0**, incluída em `engine\`, corrige essa falha. O Bahama Studio foi feito para:

- instalar essa versão sem exigir edição manual de registro;
- avisar antes de qualquer operação destrutiva;
- criar backup de tudo o que substitui;
- validar o Explorador após instalar e **reverter automaticamente** se ele não abrir pasta;
- permitir desativar/remover a extensão mesmo com o Explorador instável.

---

## Requisitos

- Windows 10 (build 10240 ou superior) ou Windows 11, 64 bits
- .NET Framework 4.8 (já presente no Windows 10/11 atualizados)
- Nenhuma dependência externa: não usa NuGet, pacotes baixados nem ferramentas de terceiros

A instalação do aplicativo **não pede administrador**. O UAC aparece apenas quando você clicar em
**Instalar engine** para registrar a DLL em `Program Files`.

Mica nativo requer Windows 11. Em Windows 10 a engine funciona normalmente com **Acrílico** ou
**Blur puro**; a interface mostra avisos claros de que o Mica não está disponível.

---

## Instalar noutra máquina

O pacote de distribuição é `dist\BahamaStudio-1.0-x64.zip`. Ele contém o executável, a engine
oficial, a licença, este README e os scripts `install.cmd` / `uninstall.cmd`.

1. Copie o ZIP para o outro PC e **extraia tudo** para uma pasta (por exemplo, Downloads).
2. Execute `install.cmd` com duplo clique. Não precisa de administrador:
   - copia os arquivos para `%LOCALAPPDATA%\Programs\Bahama Studio`;
   - cria o atalho **Bahama Studio** no Menu Iniciar;
   - registra a desinstalação apenas no `HKCU` (sem tocar no `HKLM`).
3. Abra o Bahama Studio pelo Menu Iniciar e use **Diagnóstico → Detetar e adaptar ao PC**.
   O instalador também cria um atalho na pasta Inicialização: nesse modo a app arranca **apenas
   na bandeja**, sem mostrar a janela, e reaplica os efeitos lidos do `config.ini`.
4. Vá a **Instalar** e clique em **Instalar engine 2.1.0**. Aí sim o Windows pedirá UAC.

Para remover, execute `uninstall.cmd` (está na pasta instalada e no ZIP). Ele apaga os arquivos
copiados, o atalho e a entrada de desinstalação. Os dados do usuário
(`%LOCALAPPDATA%\BahamaStudio`: configurações, logs e backups) são preservados de propósito.

Também é possível usar o ZIP em modo portátil: basta executar `BahamaStudio.exe` diretamente da
pasta extraída, sem instalar.

---

## Deteção automática e adaptação ao PC

Na primeira execução, quando ainda não existe `config.ini`, o Bahama Studio lê o computador e
cria uma configuração adequada a ele. O botão **Detetar e adaptar ao PC** (página Diagnóstico)
faz o mesmo a qualquer momento, pedindo confirmação antes de mexer numa configuração existente.

São lidos localmente:

- Windows, build completo (com UBR), edição e versão de exibição;
- arquitetura do sistema e do processo, processador, memória RAM e ecrã principal;
- tema claro/escuro, nome do computador e do usuário;
- pastas reais (`Program Files`, `LocalAppData`, logs e backups), não caminhos fixos;
- engine incluída, engine registrada, versão e se o módulo está carregado no Explorador.

A recomendação é aplicada assim:

| Sistema | Efeito recomendado | Ajustes |
| --- | --- | --- |
| Windows 11 | Acrílico | limpeza da barra WinUI ativada; cor personalizada preservada |
| Windows 10 (1809+) | Acrílico | sem barra WinUI; sem transparência total (mais estável) |
| Windows 10 anterior / outro | Blur puro | opções conservadoras |

**Nenhum `config.ini` existente é sobrescrito sem confirmação.** Depois da adaptação, as alterações
ficam pendentes na página **Aparência** para você revisar e clicar em **Aplicar** ou **Gravar**.
O relatório de **Copiar relatório** também passa a incluir esses dados do PC.

---

## Como usar

1. Abra o atalho do Menu Iniciar (ou `bin\BahamaStudio.exe` no ZIP portátil).
2. Opcionalmente, em **Diagnóstico**, clique em **Detetar e adaptar ao PC**.
3. Na página **Instalar**, clique em **Instalar engine 2.1.0** e confirme o UAC.
4. Aguarde as quatro etapas (verificar, backup, instalar, testar). O Explorador reinicia
   uma vez e uma pasta é aberta para validar o efeito.
5. Vá para **Aparência** para escolher o efeito e ajustar a transparência.

Não há serviço do Windows, telemetria nem acesso à internet. O Bahama Studio fica residente
**na bandeja do sistema** (ícone ao lado do relógio): ao fechar a janela com o `X`, ela apenas se
esconde e o reaplicador de transparência continua ativo. O menu do ícone tem **Abrir**,
**Transparência total** (liga/desliga para a sessão atual) e **Sair** (encerra de vez e remove o
efeito). O arranque automático usa esse mesmo modo bandeja.

### Atalhos

| Tecla | Ação |
| --- | --- |
| `Ctrl+1` … `Ctrl+5` | Visão geral, Aparência, Instalar, Diagnóstico, Emergência |
| `F5` | Atualizar a página atual |
| `Esc` | Fechar diálogos |
| `Ctrl+,` | Sobre |

---

## Instalação guiada — o que acontece

| Etapa | Detalhe |
| --- | --- |
| **Verificar** | Confere a DLL incluída (versão, arquitetura x64, SHA-256), a versão do Windows e o que já está registrado. |
| **Backup** | Copia DLL e `config.ini` existentes para `%LOCALAPPDATA%\BahamaStudio\backups\<data-hora>`. |
| **Instalar** | Copia a DLL, registra o COM (`regsvr32`, com fallback para registro direto) e grava `config.ini` com o efeito escolhido. |
| **Testar** | Reinicia o Explorador, abre uma pasta, confirma se o módulo `ExplorerBlurMica` está carregado e se houve novo evento de falha. |

Se o teste falhar:

- a extensão é **desregistrada**;
- o `config.ini` anterior é restaurado;
- o Explorador é reiniciado sem a extensão;
- o app explica o que aconteceu e mantém os backups.

O destino padrão é `C:\Program Files\ExplorerBlurMica.dll` (pasta usada pela engine oficial).
Se já existir um registro apontando para outro caminho, o app adota esse caminho em vez de criar
uma segunda instalação.

### Reinstalar com o Explorador em execução

O Windows não apaga uma DLL carregada, mas permite **renomeá-la**. Ao reinstalar, o Bahama Studio
move a DLL em uso para `ExplorerBlurMica.old-<data>.dll`, copia a nova e registra o caminho oficial.
As cópias antigas são apagadas automaticamente na próxima instalação, depois que o Explorador
reinicia.

### Gravação do `config.ini`

`C:\Program Files` não é gravável por usuário padrão. O Bahama Studio tem duas opções:

- **Ajustes de cor sem UAC** (opção da instalação): libera escrita no arquivo para o seu usuário
  via `icacls`, para que testar cores não peça confirmação a cada vez;
- **sem liberar**: cada gravação usa um helper elevado e pede UAC.

Você pode revogar a liberação removendo a permissão do arquivo pelas propriedades do Windows.

---

## Aparência

Cinco efeitos, mapeados para o `config.ini` da engine:

| Efeito | Valor | Cor personalizada | Observação |
| --- | --- | --- | --- |
| Blur (legado) | `0` | sim | compatibilidade |
| Acrílico | `1` | sim — **recomendado** | funciona também no Windows 10 |
| Mica | `2` | não | usa as cores do sistema no Windows 11 24H2+ |
| Blur puro | `3` | não | somente o desfoque |
| Mica Alt | `4` | não | variação do Mica |

### Personalização manual

A seção **Personalização manual** tem um controle de **transparência da cor (A)**, de `0` a `255`,
com atalhos para `0 · invisível`, `64`, `128`, `192` e `255 · sólida`:

- `a=0` deixa a tinta **totalmente invisível** (só o desfoque do Windows aparece);
- `a=255` deixa a tinta **quase sólida**;
- valores intermediários misturam a cor escolhida com o papel de parede.

Os controles RGB e o nível A valem separadamente para os temas claro e escuro. A prévia ao lado
mostra o resultado antes de aplicar; o botão **Aplicar** grava o `config.ini` e reinicia o
Explorador.

### Vidro totalmente transparente (RGBA 0,0,0,0)

O cartão **Vidro totalmente transparente** aplica, de uma só vez, o Acrílico com
**RGBA(0, 0, 0, 0)** nos temas claro e escuro:

```ini
[config]
effect=1
[light]
r=0
g=0
b=0
a=0
[dark]
r=0
g=0
b=0
a=0
```

- opacidade **zero**: nenhuma tinta cobre o vidro;
- o desfoque (Acrílico) continua ativo — é o **glassmorphism** puro, com o papel de parede
  aparecendo atrás da janela;
- o chip do cartão mostra **ATIVO** quando a configuração em edição corresponde exatamente a
  RGBA(0,0,0,0);
- na seção de personalização manual, o atalho **RGBA 0,0,0,0** zera R, G, B e A apenas do tema
  em edição (claro ou escuro).

> **Persistência:** a engine `ExplorerBlurMica` (carregada dentro do `explorer.exe`) mantém a
> configuração Acrílico/RGBA(0,0,0,0) mesmo com o Bahama Studio fechado. No Windows 11 24H2 o
> próprio Explorador volta a pintar um fundo opaco por cima do vidro durante o uso normal. Por isso
> mantenha também o interruptor **Fundo do Explorador / transparência total** ligado: ele reaplica
> o efeito enquanto a app está aberta (na bandeja no arranque, graças ao atalho na pasta Inicialização).
> Assim você tem base persistente + vidro visível o tempo todo.

> Em computadores/Drivers em que o Windows não consegue desfocar o fundo (por exemplo, GPUs
> antigas), o DWM pode cair para um fundo sólido. Nesse caso experimente **Blur puro** ou o
> **Acrílico** com um A baixo (8 a 32) para manter a sensação de vidro.

### Transparência total (nativa)

O interruptor **Transparência total** no cartão **Fundo do Explorador** aplica, diretamente nas
janelas `CabinetWClass`, uma composição com `GradientColor` controlado pelo nível escolhido e
estende a moldura DWM (`DwmExtendFrameIntoClientArea`, margens `-1`). Em **100%** o gradiente é
exatamente `0x00000000`: alfa zero, sem nenhuma tinta por cima do papel de parede.

O nível vai de **0% a 100%**:

- **100%** — remove toda a tinta (transparência total);
- **0%** — fundo sólido no tom do tema atual;
- valores intermédios misturam o fundo do tema com o papel de parede.

O estado fica em `config.ini`:

```ini
[config]
totalTransparency=true
totalTransparencyLevel=100
```

Pontos importantes:

- o efeito é **reaplicado** quando a janela é ativada, restaurada, minimizada ou perde o foco,
  e também em janelas abertas depois (`SetWinEventHook` + enumeração de janelas);
- um verificador de baixa frequência reaplica o efeito se o Explorador repintar o fundo sozinho;
- ao desligar o interruptor ou escolher **Sair** no ícone da bandeja, o efeito é removido e as
  janelas voltam ao padrão do Windows. Fechar apenas a janela com o `X` não remove o efeito: a app
  fica na bandeja e continua a reaplicá-lo;
- não reinicia o Explorador e não exige administrador.

> **Limite do Windows 11 24H2:** o conteúdo XAML do Explorador pode repintar um fundo sólido por
> cima da composição nativa. Nesse caso a moldura e o título ficam transparentes, mas a área de
> arquivos pode continuar opaca. A alternativa mais confiável para o conteúdo é o cartão
> **Vidro totalmente transparente** (Acrílico com `a=0`), que atua por dentro do Explorer.

O `config.ini` é lido da **mesma pasta da DLL** (`C:\Program Files`), não da pasta do executável.
Por isso o app sempre grava ao lado da engine instalada.

Antes de cada aplicação, o `config.ini` atual é copiado para a pasta de backups.


---

## Diagnóstico

- Versão do Windows e build, arquitetura, processador, RAM, ecrã, suporte a Mica, tema do sistema;
- estado da engine: versão, caminho, CLSID, FolderExtension, SHA-256, integridade;
- Explorador: processo, instâncias, módulo carregado, dumps;
- falhas do `explorer.exe` dos últimos 7 dias (evento 1000: módulo e código);
- verificações em lista, com o que é crítico e o que é apenas aviso;
- **Detetar e adaptar ao PC** aplica as definições recomendadas ao sistema atual;
- **Copiar relatório** gera um texto local com esses dados para colar em um chamado.

Nada é enviado para a internet.

---

## Emergência

Se o Explorador não abrir ou travar:

1. **Ao abrir uma pasta, segure `ESC`.** A engine ignora o efeito enquanto `ESC` está pressionado.
   Com a janela aberta, vá ao Bahama Studio e desative a extensão.
2. **Reiniciar o Explorer** (botão da página ou Gerenciador de Tarefas → Explorador do Windows →
   Reiniciar).
3. **Abrir o Explorer manualmente**: `Win+R` → `explorer.exe`. Se nada abrir, `Ctrl+Shift+Esc` →
   Arquivo → Executar nova tarefa → `explorer.exe`.
4. **Desativar a extensão**: desregistra a DLL e mantém os arquivos no disco.
5. **Remover tudo**: desregistra e apaga DLL, `config.ini` e arquivos de licença.
6. Se nada funcionar, reinicie o Windows. Em último caso, o **Modo de Segurança** permite abrir o
   Bahama Studio e desativar a extensão sem carregá-la.

A página também lista os backups e permite restaurar o `config.ini` mais recente.

---

## Onde ficam os arquivos

| Conteúdo | Caminho |
| --- | --- |
| Aplicativo no ZIP | `bin\BahamaStudio.exe` |
| Aplicativo instalado | `%LOCALAPPDATA%\Programs\Bahama Studio\BahamaStudio.exe` |
| Engine incluída | `engine\ExplorerBlurMica.dll` (também em `bin\engine\`) |
| Engine instalada | `%ProgramFiles%\ExplorerBlurMica.dll` (normalmente `C:\Program Files`) |
| Configuração ativa | pasta da DLL instalada (`config.ini`) |
| Instalador / desinstalador | `install.cmd` / `uninstall.cmd` na raiz do ZIP |
| Atalho no Menu Iniciar | `%APPDATA%\Microsoft\Windows\Start Menu\Programs\Bahama Studio.lnk` |
| Preferências do app | `%LOCALAPPDATA%\BahamaStudio\settings.ini` |
| Log | `%LOCALAPPDATA%\BahamaStudio\logs\bahama-studio.log` |
| Backups | `%LOCALAPPDATA%\BahamaStudio\backups\` |

Quem usava uma versão anterior tem os dados migrados automaticamente para a nova pasta do Bahama Studio.

O log registra cada operação (instalação, aplicação, reversão) e qualquer exceção da interface.

---

## Compilando

Sem Visual Studio e sem .NET SDK. Usa o compilador do .NET Framework:

```bat
build.cmd
```

O script compila `src\*.cs` em `bin\BahamaStudio.exe` (x64, WPF, .NET Framework 4.8), embute
`Theme.xaml` e o ícone, e copia `engine\` para `bin\`. Para gerar o pacote de distribuição,
atualize `dist\BahamaStudio-1.0-x64.zip` com `BahamaStudio.exe`, `BahamaStudio.exe.config`,
`engine\`, `README.md`, `install.cmd` e `uninstall.cmd`.

### Modo de autoteste

```bat
bin\BahamaStudio.exe --selftest --screenshots="C:\temp\shots"
bin\BahamaStudio.exe --selftest --size=1040x700 --screenshots="C:\temp\shots-min"
```

Abre a janela, percorre as cinco páginas, grava PNGs, uma auditoria de layout
(transbordo horizontal, texto truncado) e `selftest.txt`, e fecha sozinho.


### Operações elevadas

O próprio executável executa operações administrativas quando iniciado com `--op=...`
(`install`, `uninstall`, `writeconfig`, `grant`, `revoke`). A interface chama essas operações
com elevação apenas quando necessário; reiniciar o Explorador não exige administrador.

---

## Segurança e privacidade

- Sem rede, sem telemetria, sem atualização automática.
- Toda alteração no sistema é registrada no log local.
- Backup antes de sobrescrever; reversão automática quando o teste falha.
- Confirmação explícita para desativar, remover e reinstalar.

---

## Créditos e licença

- **Bahama Studio** — interface e orquestração de instalação, criado por **Clávio Bahama**.
  Não altera o código da engine.
- **ExplorerBlurMica** — engine oficial de [Maplespe](https://github.com/Maplespe/ExplorerBlurMica),
  licenciada em **LGPLv3**. A licença está em `engine\LICENSE-LGPLv3.txt` e é copiada para a
  pasta de instalação.

A DLL em `engine\` é distribuída sem modificações. Se você substituir a engine, mantenha a
licença e o ReadMe junto dela.

---

## Limitações conhecidas

- O Windows pode reativar o efeito após atualizações grandes; reinstale pela página Instalar.
- Antivírus podem sinalizar o registro COM em massa; o app usa `regsvr32` oficial e registra
  manualmente só quando ele falha.
- Em Windows 11 24H2+, Mica e Mica Alt usam as cores do sistema e ignoram a cor personalizada.
  Para ver a sua cor, use **Acrílico** ou **Blur (legado)**.
- Em Windows 10 não há Mica nativo, mas o resto funciona: use **Acrílico** (build 1809+) ou
  **Blur puro**. O app avisa isso no Diagnóstico, na Visão geral, na Instalação e na própria
  página Aparência ao selecionar Mica; a adaptação automática já escolhe um desses efeitos.
- O efeito é aplicado pelo Explorador; reiniciar o `explorer.exe` é necessário para ver mudanças.
