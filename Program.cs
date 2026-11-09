// Copyright (C) 2026 Davyd Stalytenko
// under the GPL-3


using System.Text;
using System.Runtime.InteropServices;


struct Note {
    private int posX = 0;
    private int posY = 0;

    public string title = "";
    public List<string> body = [];

    public Note() {}

    public int getX() { return this.posX; }
    public int getY() { return this.posY; }

    public Note(Note other) {
        this.title = other.title;
        this.body = [..other.body];
    }

    public void cropTitle(int maxLength) {
        if (this.title.Length > maxLength) {
            this.title = this.title.Substring(0, maxLength);
        }
    }

    public void up() {
        if (this.posY == 0) {
            return;
        }

        this.posY--;

        var maxLength = this.posY == 0 ? this.title.Length : this.body[this.posY - 1].Length;
        var newX = maxLength == 0 ? 0 : maxLength - 1;
        if (this.posX >= maxLength) this.posX = newX;
    }

    public void down() {
        if (this.posY < this.body.Count) {
            this.posY++;

            var maxLength = this.body[this.posY - 1].Length;
            var newX = maxLength == 0 ? 0 : maxLength - 1;
            if (this.posX >= maxLength) this.posX = newX;
        }
    }

    public void left() {
        if (this.posX == 0) {
            return;
        }

        this.posX--;
    }

    public void right() {
        int maxLength = this.posY == 0 ? this.title.Length : this.body[this.posY - 1].Length;

        if (this.posX >= maxLength) {
            return;
        }

        this.posX++;
    }

    public void enter() {
        this.body.Insert(this.posY, "");
        this.down();
    }
    
    public void push(char symbol) {
        if (this.posY == 0) {
            this.title = this.title.Insert(this.posX, symbol.ToString());
        } else {
            this.body[this.posY - 1] = this.body[this.posY - 1].Insert(this.posX, symbol.ToString());
        }

        this.posX++;
    }

    public void pop() {
        // pop begin of line
        if (this.posX == 0) {

            if (this.posY == 0) {
                // no more line to pop
                return;
            }

            // pop the line
            this.body.RemoveAt(this.posY - 1);

            // move cursor
            this.up();

            return;
        }

        string line = this.posY == 0 ? this.title : this.body[this.posY - 1];

        if (this.posX == line.Length) line = line.Remove(this.posX - 1);

        if (this.posY == 0) {
            this.title = this.title = line;
        } else {
            this.body[this.posY - 1] = line;
        }

        this.left();
    }
}


enum State {
    home,
    create,
    edit,
    exit
}


class Model {
    List<Note> notes = [];

    Note currentNote; // for show|edit
    int currentTitleIdx = 0; // for home

    State state = State.home; // main state

    public Model() {}

    public void createNote() {
        if (this.state == State.home) {
            this.state = State.create;
            this.currentNote = new Note();
        }
    }

    /// show note | navigate note
    public void enter() {
        switch (this.state) {
            case State.home: break; // show note
            case State.edit: // navigate note
                this.currentNote.enter();
                return;
            case State.create: // navigate note
                this.currentNote.enter();
                return;
            default: return;
        }

        if (this.currentTitleIdx >= this.notes.Count) {
            return;
        }

        this.state = State.edit;
        this.currentNote = new Note(notes[currentTitleIdx]);
    }

    public void cancel() {
        switch (this.state) {
            case State.edit: break;
            case State.create: break;
            case State.home:
                this.state = State.exit;
                return;
            default: return;
        }

        this.state = State.home;
    }

    public void save() {
        if (this.state == State.edit) {
            this.notes[this.currentTitleIdx] = this.currentNote;

            this.state = State.home;
            return;
        }

        if (this.state == State.create) {
            this.notes.Add(this.currentNote);
            this.state = State.home;
        }
    }

    public void up() {
        if (this.state == State.home) {
            if (this.currentTitleIdx != 0) {
                this.currentTitleIdx--;
            }

            return;
        }

        this.currentNote.up();
    }

    public void down() {
        if (this.state == State.home) {
            if (this.currentTitleIdx < this.notes.Count - 1) { // fix bug
                this.currentTitleIdx++;
            }

            return;
        }

        this.currentNote.down();
    }

    public List<string> getTitles() { return this.notes.Select(item => item.title).ToList(); }
    public int currentTitle() { return this.currentTitleIdx; }
    public State getState() { return this.state; }

    public ref Note getNote() { return ref this.currentNote; }
}


class View {
    private Model model;

    private char[] buffer;
    private int w;
    private int h;
    private int size;

    private int lastTitle = 0;
    private int titleShift = 0;

    private int lastLine = 0;
    private int lineShift = 0;

    private bool cursor = false;
    private int cursorX = 0;
    private int cursorY = 0;

    public View(Model model) {
        this.model = model;
        this.resize();
    }

    public void resize() {
        this.w = Console.BufferWidth;
        this.h = Console.BufferHeight;
        this.size = this.w * this.h;

        this.buffer = new char[this.size];
    }

    public void render() {
        this.cursor = false;

        this.drawState();

        StringBuilder sb = new(this.w * this.h + this.h);

        for (int y = 0; y < this.h - 1; y++) {
            sb.Append(this.buffer, this.w * y, this.w);
            sb.AppendLine();
        }

        sb.Append(this.buffer, this.w * (this.h - 1), this.w);

        Console.Clear();
        Console.Write(sb.ToString());
        Console.CursorVisible = this.cursor;

        if (this.cursor) {
            Console.SetCursorPosition(this.cursorX, this.cursorY);
        }
    }

    private bool drawBase() {
        // init
        for (int idx = 0; idx < this.w * this.h; idx++) {
            this.buffer[idx] = ' ';
        }

        // drow left side
        for (int y = 0; y < this.h; y++) {
            // x = 0
            this.buffer[this.w * y] = '#';
        }

        // drow top side
        for (int x = 0; x < this.w; x++) {
            // y = 0
            this.buffer[x] = '#';
        }
        
        // drow bottom side
        for (int x = 0; x < this.w; x++) {
            // y = 0
            this.buffer[this.w * (this.h - 1) + x] = '#';
        }
        
        // drow right side
        for (int y = 0; y < this.h; y++) {
            // x = w - 1
            this.buffer[this.w * y + this.w - 1] = '#';
        }

        if (this.w < 45 || this.h < 14) {
            this.drawCText("FAIL", 1, 1, this.w - 2);
            return false;
        }

        this.drawCText("Copyright (C) 2026 Davyd Stalytenko", 1, 1, this.w - 2);
        this.drawCText("-----------------------------------", 1, 2, this.w - 2);
        this.drawCText("NOTE-BOOK T 1000", 1, 3, this.w - 2);
        this.drawLine('*', 1, 4, this.w - 2);
        this.drawLine('-', 1, this.h - 3, this.w - 2);

        return true;
    }

    private void drawCText(String text, int x, int y, int size) {
        if (text.Length >= size) {
            for (int idx = 0; idx < text.Length; idx++) {
                int at = this.w * y + x + idx;
                if (at >= this.size) return;

                this.buffer[at] = text[idx];
            }

            return;
        }

        var shift = (size - text.Length) / 2;
            
        for (int idx = 0; idx < text.Length; idx++) {
            int at = this.w * y + x + shift + idx;
            if (at >= this.size) return;

            this.buffer[at] = text[idx];
        }
    }
    
    private void drawMText(String text, int x, int y, int size) {
        for (int idx = 0; idx < text.Length; idx++) {
            int relativeX = idx % size;

            if (relativeX == 0 && idx != 0) y++;

            int at = this.w * y + x + relativeX;
            if (at >= this.size) return;

            this.buffer[at] = text[idx];
        }
    }

    private void drawLine(char symbol, int x, int y, int size) {
        for (int idx = 0; idx < size; idx++) {
            int at = this.w * y + x + idx;
            if (at >= this.size) return;

            this.buffer[at] = symbol;
        }
    }

    private int calcTextLines(String text, int size) {
        int len = text.Length / size;

        if (text.Length % size != 0) {
            return len + 1;
        }

        return len;
    }

    private void drawState() {
        if (!this.drawBase()) return;

        switch (this.model.getState()) {
            case State.home:
                this.drawCText("-> HOME", 2, 2, 0);
                this.drawList();
                this.drawCText("<ESC>: exit; <up/down>: move; <enter>: edit; <C-N>: new", 1, this.h - 2, this.w - 2);
                break;
            case State.edit:
                this.drawCText("-> EDIT", 2, 2, 0);
                this.drawCText("<up/down/enter>: edit; <ESC>: cancel; <C-S>: save", 1, this.h - 2, this.w - 2);
                this.drawNote();
                break;
            case State.create:
                this.drawCText("-> CREATE", 2, 2, 0);
                this.drawCText("<up/down/enter>: edit; <ESC>: cancel; <C-S>: save", 1, this.h - 2, this.w - 2);
                this.drawNote();
                break;
            default:
                this.drawCText("-> <invalid; error>", 2, 2, 0);
                this.drawCText("invalid state; contact support", 1, this.h - 2, this.w - 2);
                break;
        }
    }

    private void drawList() {
        var titles = this.model.getTitles();
        int size = this.w - 4;

        int y = 5;
        int freeHeight = this.h - 8;

        if (this.model.currentTitle() > this.lastTitle) {
            this.titleShift++;
        }

        if (this.model.currentTitle() < this.titleShift) {
            this.titleShift--;
        }

        int idx = this.titleShift;

        for (; idx < titles.Count; idx++) {
            var choosen = this.model.currentTitle() == idx ? "->" : "";
            var line = $"{choosen} {idx + 1}. {titles[idx]}";
            var count = this.calcTextLines(line, size);

            freeHeight -= count;
            
            // make space if there free lines and not a first loop
            if (freeHeight > 0 && idx != 0) {
                freeHeight--;
                y++;
            }

            if (freeHeight < 0) {
                this.lastTitle = idx - 1; // we could not render that idx, shift window down
                return;
            }

            this.drawMText(line, 2, y, this.w - 4);
            y += count;
        }

        this.lastTitle = idx; // shift window down
    }

    private void drawNote() {
        var note = this.model.getNote();
        var lines = note.body;

        int size = this.w - 4;

        int y = 5 ; // where to start

        // draw title
        this.drawMText($"  TITLE: {note.title}", 2, y, this.w - 4);

        y += this.calcTextLines(note.title, size);

        this.drawLine('-', 1, y++, this.w - 2);

        int freeHeight = this.h - y - 3; // 3 for the bottom

        this.cursor = true;
        
        // cursor is on title
        if (note.getY() == 0) {
            this.cursorX = note.getX() + 11;
            this.cursorY = 5;
        }

        // nothing there yet
        if (lines.Count == 0) {
            this.drawCText("Start writting your note...", 2, y + 4, size);
            return;
        }

        var currentLine = note.getY() == 0 ? 0 : note.getY() - 1;

        // cursor is on the note
        if (note.getY() > 0) {
            this.cursorX = note.getX() + 4;
           this.cursorY = currentLine + y;
        }
        
        if (currentLine > this.lastLine && this.lineShift < lines.Count - 1) {
            this.lineShift++;
        }

        if (currentLine < this.lastLine && this.lineShift > 0) {
            this.lineShift--;
        }

        int idx = this.lineShift; // ensure no index oor while deletion and back-move 

        // idx = Math.Max(this.lineShift, 0);

        for (; idx < lines.Count; idx++) {
            var choosen = currentLine == idx ? ":" : " ";
            var line = $"{choosen} {lines[idx]}";
            var count = this.calcTextLines(line, size);

            freeHeight -= count;
            
            if (freeHeight < 0) {
                this.lastLine = idx - 1; // we could not render that idx, shift window down
                return;
            }

            this.drawMText(line, 2, y, this.w - 4);
            y += count;
        }

        this.lastLine = idx; // shift window down
    }
}


class Controller {
    private PosixSignalRegistration posix;
    private Model model;
    private View view;

    public Controller() {
        this.model = new();
        this.view = new(this.model);

        this.posix = PosixSignalRegistration.Create(
            PosixSignal.SIGWINCH,
            ctx => {
                this.view.resize();
                this.view.render();
            }
        );
    }

    public void run() {
        while (this.model.getState() != State.exit) {
            this.view.render();
            this.processKeys();
        }
    }

    private void processKeys() {
        ConsoleKeyInfo key = Console.ReadKey(true);

        switch (key.Key) {
            case ConsoleKey.UpArrow:
                this.model.up();
                return;
            case ConsoleKey.DownArrow:
                this.model.down();
                return;
            case ConsoleKey.Escape:
                this.model.cancel();
                return;
            case ConsoleKey.Enter:
                this.model.enter();
                return;
        }

        var state = this.model.getState();

        if (state != State.home)
        {
            this.processKeysInNote(key);
            return;
        }
        
        // in home, create note
        if (state == State.home &&
        key.Key == ConsoleKey.N &&
        key.Modifiers == ConsoleModifiers.Control)
        {
            this.model.createNote();
            return;
        }        
    }

    private void processKeysInNote(ConsoleKeyInfo key) {
        switch (key.Key) {
            case ConsoleKey.LeftArrow:
                this.model.getNote().left();
                return;
            case ConsoleKey.RightArrow:
                this.model.getNote().right();
                return;
            case ConsoleKey.Backspace:
                this.model.getNote().pop();
                return;
        }

        // check for save key
        if (key.Key == ConsoleKey.S && key.Modifiers == ConsoleModifiers.Control)
        {
            this.model.save();
            return;
        }

        // any symbol
        if (!char.IsControl(key.KeyChar)) {
            this.model.getNote().push(key.KeyChar);
        }
    }

    static public void Main(string[] args) {
        var c = new Controller();
        c.run();
    }
}
