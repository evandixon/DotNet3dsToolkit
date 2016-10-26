Namespace Misc
    ''' <summary>
    ''' Runs a provided delegate function or sub repeatedly and asynchronously in the style of a For statement.
    ''' </summary>
    Public Class AsyncFor

        Public Delegate Sub ForItem(i As Integer)
        Public Delegate Sub ForEachItem(Of T)(i As T)
        Public Delegate Function ForEachItemAsync(Of T)(i As T) As Task
        Public Delegate Function ForItemAsync(i As Integer) As Task
        Public Event LoadingStatusChanged(sender As Object, e As LoadingStatusChangedEventArgs)

#Region "Constructors"
        Public Sub New()
            BatchSize = Integer.MaxValue
            RunningTasks = New List(Of Task)
        End Sub
        <Obsolete> Public Sub New(ProgressMessage As String)
            Me.New
        End Sub
#End Region

#Region "Properties"

        ''' <summary>
        ''' Whether or not to run each task sequentially.
        ''' </summary>
        ''' <returns></returns>
        Public Property RunSynchronously As Boolean

        ''' <summary>
        ''' The number of tasks to run at once.
        ''' </summary>
        ''' <returns></returns>
        Public Property BatchSize As Integer

        ''' <summary>
        ''' The currently running tasks.
        ''' </summary>
        ''' <returns></returns>
        Private Property RunningTasks As List(Of Task)

        ''' <summary>
        ''' The total number of tasks to run.
        ''' </summary>
        ''' <returns></returns>
        Private Property TotalTasks As Integer

        ''' <summary>
        ''' The number of tasks that have been completed.
        ''' </summary>
        ''' <returns></returns>
        Private Property CompletedTasks As Integer
            Get
                Return _completedTasks
            End Get
            Set(value As Integer)
                _completedTasks = value
                RaiseEvent LoadingStatusChanged(Me, New LoadingStatusChangedEventArgs With {.Complete = (value = TotalTasks),
                                                .Completed = value,
                                                .Progress = If((TotalTasks > 0), (value / TotalTasks), 1),
                                                .Total = TotalTasks})
            End Set
        End Property
        Dim _completedTasks As Integer

#End Region

#Region "Core Functions"
        Public Async Function RunForEach(Of T)(DelegateFunction As ForEachItemAsync(Of T), Collection As IEnumerable(Of T)) As Task
            'Todo: throw exception if there's already tasks running
            Dim taskItemQueue As New Queue(Of T)
            For Each item In Collection
                taskItemQueue.Enqueue(item)
            Next

            TotalTasks = taskItemQueue.Count

            'While there's either more tasks to start or while there's still tasks running
            While (taskItemQueue.Count > 0 OrElse (taskItemQueue.Count = 0 AndAlso RunningTasks.Count > 0))
                If RunningTasks.Count < BatchSize AndAlso taskItemQueue.Count > 0 Then
                    'We can run more tasks

                    'Get the next task item to run
                    Dim item = taskItemQueue.Dequeue 'The item in Collection to process

                    'Start the task
                    Dim tTask = Task.Run(Async Function() As Task
                                             Await DelegateFunction(item)
                                             System.Threading.Interlocked.Increment(CompletedTasks)
                                         End Function)

                    'Either wait for it or move on
                    If RunSynchronously Then
                        Await tTask
                    Else
                        RunningTasks.Add(tTask)
                    End If
                Else
                    If RunningTasks.Count > 0 Then
                        'We can't start any more tasks, so we have to wait on one.
                        Await Task.WhenAny(RunningTasks)

                        'Remove completed tasks
                        For count = RunningTasks.Count - 1 To 0 Step -1
                            If RunningTasks(count).GetAwaiter.IsCompleted Then
                                RunningTasks.RemoveAt(count)
                            End If
                        Next
                    Else
                        'We're finished.  Nothing else to do.
                        Exit While
                    End If
                End If
            End While
        End Function

        Public Async Function RunFor(DelegateFunction As ForItemAsync, StartValue As Integer, EndValue As Integer, Optional StepCount As Integer = 1) As Task
            'Todo: throw exception if there's already tasks running

            If StepCount = 0 Then
                Throw New ArgumentException(My.Resources.Language.ErrorAsyncForInfiniteLoop, NameOf(StepCount))
            End If

            'Find how many tasks there are to run
            'The +1 here makes the behavior "For i = 0 to 10" have 11 loops
            TotalTasks = Math.Ceiling((EndValue - StartValue + 1) / StepCount)

            If TotalTasks < 0 Then
                'Then in a normal For statement, the body would never be called
                TotalTasks = 0
                CompletedTasks = 0
                Exit Function
            End If

            Dim i As Integer = StartValue

            Dim tasksRemaining As Integer = TotalTasks 'The tasks that still need to be queued

            'While there's either more tasks to start or while there's still tasks running
            While (tasksRemaining > 0 OrElse (tasksRemaining = 0 AndAlso RunningTasks.Count > 0))
                If RunningTasks.Count < BatchSize AndAlso tasksRemaining > 0 Then
                    'We can run more tasks

                    Dim item = i 'To avoid async weirdness with having this in the below lambda

                    'Start the task
                    Dim tTask = Task.Run(Async Function() As Task
                                             Await DelegateFunction(item)
                                             System.Threading.Interlocked.Increment(CompletedTasks)
                                         End Function)

                    'Increment for the next run
                    i += StepCount

                    'Either wait for it or move on
                    If RunSynchronously Then
                        Await tTask
                    Else
                        RunningTasks.Add(tTask)
                    End If
                    tasksRemaining -= 1
                Else
                    If tasksRemaining > 0 Then
                        'We can't start any more tasks, so we have to wait on one.
                        Await Task.WhenAny(RunningTasks)

                        'Remove completed tasks
                        For count = RunningTasks.Count - 1 To 0 Step -1
                            If RunningTasks(count).GetAwaiter.IsCompleted Then
                                RunningTasks.RemoveAt(count)
                            End If
                        Next
                    Else
                        'We're finished.  Nothing else to do.
                        Exit While
                    End If
                End If
            End While
        End Function

        Public Async Function RunFor(DelegateSub As ForItem, StartValue As Integer, EndValue As Integer, Optional StepCount As Integer = 1) As Task
            Await RunFor(Function(Count As Integer) As Task
                             DelegateSub(Count)
                             Return Task.FromResult(0)
                         End Function, StartValue, EndValue, StepCount)
        End Function

        Public Async Function RunForEach(Of T)(DelegateSub As ForEachItem(Of T), Collection As IEnumerable(Of T)) As Task
            Await RunForEach(Function(Item As T) As Task
                                 DelegateSub(Item)
                                 Return Task.FromResult(0)
                             End Function, Collection)
        End Function
#End Region

    End Class
End Namespace

