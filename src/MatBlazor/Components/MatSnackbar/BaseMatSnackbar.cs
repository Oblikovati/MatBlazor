﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace MatBlazor
{
    /// <summary>
    /// Snackbars provide brief messages about app processes at the bottom of the screen.
    /// </summary>
    public class BaseMatSnackbar : BaseMatDomComponent
    {
        private bool _isOpen;
        private CancellationTokenSource _timeoutCts;

        [Parameter]
        public RenderFragment ChildContent { get; set; }

        [Parameter]
        public bool Stacked { get; set; }

        [Parameter]
        public bool Leading { get; set; }

        [Parameter]
        public bool IsOpen
        {
            get => _isOpen;
            set
            {
                if (IsOpen != value)
                {
                    _isOpen = value;
                    CallAfterRender(async () => await SetIsOpen(value));
                    if (_isOpen == false)
                        _timeoutCts?.Cancel(false);
                    else if (_isOpen && Timeout >= 0)
                    {
                        if (_timeoutCts != null)
                        {
                            _timeoutCts.Cancel(false);
                            _timeoutCts.Dispose();
                        }
                        _timeoutCts=new CancellationTokenSource();
                        Task.Delay(Timeout, _timeoutCts.Token).ContinueWith(task =>
                        {
                            if (_timeoutCts.IsCancellationRequested) // <-- we were closed before the timeout, so don't close
                                return;
                            _isOpen = false;
                            SetIsOpen(false);
                        });
                    }
                }
            }
        }

        private async Task SetIsOpen(bool value)
        {
            await JsInvokeAsync<object>("matBlazor.matSnackbar.setIsOpen", Ref, value);
        }

        /// <summary>
        /// Timeout in ms after which the snackbar closes itself. Default: 10000 ms
        /// To leave the snackbar open indefinitely set the timeout to -1
        /// </summary>
        [Parameter]
        public int Timeout { get; set; } = 10000; // ms

        [Parameter]
        public EventCallback<bool> IsOpenChanged { get; set; }


        private DotNetObjectReference<BaseMatSnackbar> dotNetObjectRef;

        public BaseMatSnackbar()
        {
            ClassMapper
                .Add("mdc-snackbar")
                .If("mdc-snackbar--stacked", () => Stacked)
                .If("mdc-snackbar--leading", () => Leading);
            CallAfterRender(async () =>
            {
                dotNetObjectRef = dotNetObjectRef ?? CreateDotNetObjectRef(this);
                await JsInvokeAsync<object>("matBlazor.matSnackbar.init", Ref, dotNetObjectRef);
            });
        }

        public override void Dispose()
        {
            base.Dispose();
            DisposeDotNetObjectRef(dotNetObjectRef);
            _timeoutCts?.Cancel(false);
            _timeoutCts?.Dispose();
        }

        [JSInvokable]
        public async Task MatSnackbarClosedHandler()
        {
            _isOpen = false;
            await IsOpenChanged.InvokeAsync(false);
            this.StateHasChanged();
        }
    }
}