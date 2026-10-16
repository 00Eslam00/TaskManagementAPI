using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManagement.Application.Abstractions;
using TaskManagement.Application.Tasks;

namespace TaskManagement.Api.Controllers;

/// <summary>Tasks of the logged-in user. A user can only see and change their own tasks.</summary>
[ApiController]
[Route("api/tasks")]
[Authorize]
[Produces("application/json")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
public class TasksController : ControllerBase
{
    private readonly ITaskService _taskService;
    private readonly ICurrentUser _currentUser;

    public TasksController(ITaskService taskService, ICurrentUser currentUser)
    {
        _taskService = taskService;
        _currentUser = currentUser;
    }

    /// <summary>Creates a task for the logged-in user. It starts as Pending.</summary>
    /// <remarks>The same title cannot be used twice by the same user on the same (UTC) day.</remarks>
    [HttpPost]
    [ProducesResponseType<TaskResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(CreateTaskRequest request, CancellationToken cancellationToken)
    {
        var task = await _taskService.CreateAsync(_currentUser.UserId, request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = task.Id }, task);
    }

    /// <summary>Returns one of the logged-in user's tasks.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<TaskResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _taskService.GetByIdAsync(_currentUser.UserId, id, cancellationToken));
    }

    /// <summary>Returns the logged-in user's tasks, sorted by priority (High first), then by creation date (oldest first).</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<TaskResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        return Ok(await _taskService.GetAllAsync(_currentUser.UserId, cancellationToken));
    }

    /// <summary>Changes the status of a task.</summary>
    /// <remarks>A task that is Done cannot be changed any more.</remarks>
    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType<TaskResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStatus(Guid id, UpdateTaskStatusRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _taskService.UpdateStatusAsync(_currentUser.UserId, id, request, cancellationToken));
    }
}
